using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    public static void Main(string[] args)
    {
        TcpClient client = new TcpClient();
        client.Connect("localhost", 21);

        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

        stream.ReadTimeout = 5000;
        stream.WriteTimeout = 5000;

        Console.WriteLine("Соединение установлено. Введите команду (LIST, DOWNLOAD, QUIT):");

        while (true)
        {
            string input = Console.ReadLine();
            string[] inputParts = input.Split(' ');
            string command = inputParts[0];

            if (command == "LIST")
            {
                writer.WriteLine("LIST");
                writer.Flush();

                while (true)
                {
                    string response = reader.ReadLine();
                    if (response == null || response.StartsWith("226"))
                        break;

                    Console.WriteLine(response);
                }

            }
            else if (command == "DOWNLOAD" && inputParts.Length == 2)
            {
                string filename = inputParts[1];
                writer.WriteLine("DOWNLOAD " + filename);
                writer.Flush();


                ReceiveFile(stream, "H:\\aa.txt");

            }
            else if (command == "SEND" && inputParts.Length == 2)
            {
                string filename = Path.GetFileName(inputParts[1]);
                string filepath = inputParts[1];

                writer.WriteLine("SEND " + filename);
                writer.Flush();

                SendFile(stream, filepath);
            }
            else if (command == "QUIT")
            {
                writer.WriteLine("QUIT");
                writer.Flush();
                break;
            }
            else
            {
                Console.WriteLine("Неверная команда.");
            }
        }

        client.Close();
    }

    static async void ReceiveFile(NetworkStream reader, string filename)
    {
        byte[] buf = new byte[65536];
        await ReadBytes(sizeof(long),reader,buf);
        long remainingLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buf, 0));

        using var file = File.Create(filename);

        while (remainingLength > 0)
        {
            int lengthToRead = (int)Math.Min(remainingLength, buf.Length);
            await ReadBytes(lengthToRead, reader, buf);
            await file.WriteAsync(buf, 0, lengthToRead);
            remainingLength -= lengthToRead;
        }

    }

    static void SendFile(NetworkStream writer,  string filePath)
    {
        if (File.Exists(filePath))
        {

            using (var fileStream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[60000];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);

                }
            }
        }
    }
    static async Task ReadBytes(int howmuch, NetworkStream stream, byte[] buf)
    {
        int readPos = 0;
        while (readPos < howmuch)
        {
            var actuallyRead = await stream.ReadAsync(buf, readPos, howmuch - readPos);
            if (actuallyRead == 0)
                throw new EndOfStreamException();
            readPos += actuallyRead;
        }
    }
}
