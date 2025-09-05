using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

// Clase para guardar la información de posición del planeta
[Serializable]
public class PosicionPlaneta
{
    public int id; // Identificador del planeta
    public float x; // Coordenada x
    public float y; // Coordenada y
}

public class TCPIPServerAsync : MonoBehaviour
{
    // Fila para guardar las posiciones recibidas
    private Queue<PosicionPlaneta> positionQueue = new Queue<PosicionPlaneta>();
    // Objeto para bloquear el acceso a la fila
    private object queueLock = new object();
    // Objeto del planeta que se va a mover
    public GameObject planeta;
    System.Threading.Thread SocketThread;
    volatile bool keepReading = false;

    void Start()
    {
        Application.runInBackground = true;
        startServer();
    }


    void startServer()
    {
        SocketThread = new System.Threading.Thread(networkCode);
        SocketThread.IsBackground = true;
        SocketThread.Start();
    }

    private string getIPAddress()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
            }

        }
        return localIP;
    }


    Socket listener;
    Socket handler;
    void networkCode()
    {
        string data;

        // Data buffer for incoming data.
        byte[] bytes = new Byte[1024];

        // host running the application.
        //Create EndPoint
        IPAddress IPAdr = IPAddress.Parse("127.0.0.1"); // Dirección IP
        IPEndPoint localEndPoint = new IPEndPoint(IPAdr, 1104);

        // Create a TCP/IP socket.
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and 
        // listen for incoming connections.

        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);

            // Start listening for connections.
            while (true)
            {
                keepReading = true;

                // Program is suspended while waiting for an incoming connection.
                Debug.Log("Waiting for Connection");     //It works

                handler = listener.Accept();
                Debug.Log("Client Connected");     //It doesn't work
                data = null;

                byte[] SendBytes = System.Text.Encoding.Default.GetBytes("I will send key");
                handler.Send(SendBytes); // dar al cliente

                // An incoming connection needs to be processed.
                while (keepReading)
                {
                    bytes = new byte[4096];
                    int bytesRec = handler.Receive(bytes);

                    if (bytesRec <= 0)
                    {
                        keepReading = false;
                        handler.Disconnect(true);
                        break;
                    }

                    data = System.Text.Encoding.UTF8.GetString(bytes, 0, bytesRec);

                    try
                    {
                        PosicionPlaneta posicionPlaneta = JsonUtility.FromJson<PosicionPlaneta>(data);
                        Debug.Log("ID: " + posicionPlaneta.id + " X: " + posicionPlaneta.x + " Y: " + posicionPlaneta.y);
                        ActualizarPosicion(posicionPlaneta);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Error deserializing JSON: " + e.Message);
                    }

                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }

                    System.Threading.Thread.Sleep(1);
                }

                System.Threading.Thread.Sleep(1);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    // Metodo para actualizar la posición del planeta utilizando la fila con lock
    private void ActualizarPosicion(PosicionPlaneta pos)
    {
        lock (queueLock) // se bloquea el acceso a la fila
        {
            positionQueue.Enqueue(pos); // se agrega la nueva posición a la fila
        }
    }

    // Metodo que mueve el planeta en cada frame si hay nuevas posiciones en la fila
    void Update()
    {
        lock (queueLock) // se bloquea el acceso a la fila
        {
            while (positionQueue.Count > 0) // mientras haya posiciones en la fila
            {
                var pos = positionQueue.Dequeue(); // se saca la siguiente posición de la fila
                // si hay un planeta asignado, se mueve a la nueva posición
                if (planeta != null)
                {
                    planeta.transform.position = new Vector3(pos.x, 0, pos.y);
                }
            }
        }
    }

    void stopServer()
    {
        keepReading = false;

        //stop thread
        if (SocketThread != null)
        {
            SocketThread.Abort();
        }

        if (handler != null && handler.Connected)
        {
            handler.Disconnect(false);
            Debug.Log("Disconnected!");
        }
    }

    void OnDisable()
    {
        stopServer();
    }
}
