﻿/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

public class ConnectionData
{
    public Socket client;
    public bool IC;
}

public class DataStreamServer : PersistentUnitySingleton<DataStreamServer> {

    public const int PORT = 9001;
    public const int ALTPORT = 9000;
    private static string recievedData;
    private static int messageLen = 4 + (4 * 15);

    private List<ConnectionData> connections;


    protected override void Awake()
    {
        base.Awake();
        connections = new List<ConnectionData>();
        var dbEndpoint = new IPEndPoint(IPAddress.Parse(NetworkController.settings.clientIp), PORT);
        var altEndpoint = new IPEndPoint(IPAddress.Parse(NetworkController.settings.altClientIP), ALTPORT);
        var dbClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var altClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        dbClient.BeginConnect(dbEndpoint, new AsyncCallback(ConnectCallback), new ConnectionData() { client = dbClient, IC = true });
        altClient.BeginConnect(altEndpoint, new AsyncCallback(ConnectCallback), new ConnectionData() { client = altClient, IC = false });


    }

    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            ConnectionData data = (ConnectionData)ar.AsyncState;
            data.client.EndConnect(ar);
            connections.Add(data);
        } catch (Exception e)
        {
            Debug.Log("DataStreamServer ConnectCallback: " + e.Message);
        }
    }

    private void SendTCP(byte[] data, Socket client)
    {
        try
        {
            if (client.Connected) {
                client.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), client);
            }
        } catch (Exception e)
        {
            Debug.Log("DataStreamServer SendTCP: " + e.Message);
            Debug.Log("Socket disconnected due to error");
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
    }

    private static void SendCallback(IAsyncResult ar)
    {
        Socket handler = (Socket)ar.AsyncState;
        try
        {
            handler.EndSend(ar);
        }
        catch (Exception e)
        {
            Debug.Log("DataStramServer SendCallback: " + e.ToString());
            Debug.Log("Socket disconnected due to error");
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var client in connections)
        {
            client.client.Shutdown(SocketShutdown.Both);
            client.client.Close();
        }
    }

    public void Send(byte[] data)
    {
        for (int i = connections.Count - 1; i >= 0; i--) {
            var client = connections[i];
            if (client.client.Connected) {
                SendTCP(data, client.client);
            } else {
                connections.RemoveAt(i);
            }
        }
    }

    public void Send(float[] data)
    {
        byte[] senddata = new byte[messageLen];
        System.Buffer.BlockCopy(data, 0, senddata, 0, messageLen);
        Send(senddata);
    }

    //send as packed struct
    public void Send(FullDataFrame data)
    {
        byte[] dataBytes = new byte[messageLen];//declare byte array and initialize its size
        System.IntPtr ptr = Marshal.AllocHGlobal(messageLen);//pointer to byte array

        Marshal.StructureToPtr(data, ptr, true);
        Marshal.Copy(ptr, dataBytes, 0, messageLen);
        Marshal.FreeHGlobal(ptr);

        Send(dataBytes);
    }

    public void Send(string csvData)
    {
        byte[] dataBytes = Encoding.ASCII.GetBytes(csvData);
        Send(dataBytes);
    }

    public void SendAsText(FullDataFrame frame)
    {
        for (int i = connections.Count - 1; i >= 0; i--) {
            var connection = connections[i];
            if (connection.client.Connected) {
                byte[] data;
                data = Encoding.ASCII.GetBytes(connection.IC ? frame.ToICCSV() : frame.ToCSV());
                SendTCP(data, connection.client);
            } else {
                connections.RemoveAt(i);
            }
        }
    }
}
