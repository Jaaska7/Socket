using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

class Server
{
    // Cadena de conexión per SQL Server en el contenidor Docker
    static string connectionString = "Data Source=localhost,1433;Initial Catalog=ScrabbleDB;User Id=sa;Password=Passw0rd!;";

    static void Main()
    {
        int port = 12346;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine("Servidor esperando conexiones en el puerto " + port);

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        bytesRead = stream.Read(buffer, 0, buffer.Length);
        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        try
        {
            JObject credentials = JObject.Parse(data);

            // Credenciales del JSON
            string username = credentials["usuario"].ToString();
            string password = credentials["contrasenya"].ToString();

            // Conexio a la base de dades i verificar les dades 
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Consulta a la base de dades
                    string query = "SELECT * FROM Jugador WHERE user_jugador = @Username AND password = @Password";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                Console.WriteLine("Client autenticat.");

                                string response = "Correcto";
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                                stream.Write(responseBuffer, 0, responseBuffer.Length);
                            }
                            else
                            {
                                Console.WriteLine("Error: Credencials incorrectas.");
                                string response = "Credenciales incorrectas";
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                                stream.Write(responseBuffer, 0, responseBuffer.Length);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al conectar a la base de dades: " + ex.Message);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: Error en el format JSON.");
            string response = "Formato JSON incorrecto";
            byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
            stream.Write(responseBuffer, 0, responseBuffer.Length);
        }

        client.Close();
    }
}
