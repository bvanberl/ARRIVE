﻿
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.IO; // to get BinaryReader
using System.Linq; // to get array's Min/Max
using System.Collections.Generic;
using System;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif


public class NetworkManager : MonoBehaviour {
    public ConnectMenuInputHandler ConnectMenu;
    public ProjectMenuInputHandler ProjectMenu;
    public ScanMenuInputHandler ScanMenu;
    public BrainMenuInputHandler BrainMenu;
    public GameObject brainCube;
    public Brain brain;
    public Loader brainLoader;
    public Text connectionText, establishedConnText;
    public Button disconnectButton;

    public static byte[] data = new byte[4];
    //public int size = 4;
#if !UNITY_EDITOR
    StreamSocket socket;
    DataWriter writer;
    DataReader reader;
#endif
    // Use this for initialization

    private void OnDestroy()
    {
#if !UNITY_EDITOR
        // dispose socket
        socket.Dispose();
#endif
    }

    // TODO: this function should accept an IP address (hard coded for testing)
    public void connect(string IPaddress)
    {
#if !UNITY_EDITOR
        connectToDesktop(IPaddress);
#endif
    }

#if !UNITY_EDITOR
    async void connectToDesktop(string IPaddress) {
        HostName hostName;
        socket = new StreamSocket();
        hostName = new HostName(IPaddress);

        // Set NoDelay to false so that the Nagle algorithm is not disabled
        socket.Control.NoDelay = false;

        try
        {
            // Connect to the server
            await socket.ConnectAsync(hostName, "11000");
                    
            // initialize reader and writer with socket
            writer = new DataWriter(socket.OutputStream);
            reader = new DataReader(socket.InputStream);
            
            // update UI
            ConnectMenu.connectionEstablished = true;

            // request projects
            getProjects();

            
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.Write(exception.ToString());

            switch (SocketError.GetStatus(exception.HResult))
            {
                case SocketErrorStatus.HostNotFound:
                    // Handle HostNotFound Error
                    throw;
                default:
                    // If this is an unknown status it means that the error is fatal and retry will likely fail.
                    throw;
            }
        }
            
        
    }
#endif

    public void getProjects()
    {
#if !UNITY_EDITOR
        requestProjects();
#endif
    }

    public void getScans(string projectName)
    {
#if !UNITY_EDITOR
        requestScans(projectName);
#endif
    }

    public void getScan(string scanName)
    {
#if !UNITY_EDITOR
        requestScan(scanName);
#endif
        
    }

    public void saveScan(string scanName, string serializedScan)
    {
#if !UNITY_EDITOR
        sendScan(scanName, serializedScan);
#endif
    }

    public void disconnect()
    {
#if !UNITY_EDITOR
        sendDisconnect();
#endif
    }


#if !UNITY_EDITOR
    async void requestProjects()
    {
        try {
            // request projects
            writer.WriteString("projects");
            await writer.StoreAsync();
            await writer.FlushAsync();
            uint size = 4;
            data = new byte[size];
            await reader.LoadAsync(size);
            reader.ReadBytes(data);
            var str = System.Text.Encoding.ASCII.GetString(data);
            uint msgSize = UInt32.Parse(str);
            System.Diagnostics.Debug.Write("MESSAGE SIZE: " + msgSize.ToString());
            writer.WriteString("projects-ready");
            await writer.StoreAsync();
            await writer.FlushAsync();

            await reader.LoadAsync(msgSize);
            data = new byte[msgSize];
            reader.ReadBytes(data);
            str = System.Text.Encoding.ASCII.GetString(data);
            string[] projects = str.Split('\n');
            ProjectMenu.projects = projects;
            ProjectMenu.projectsReadyFlag = true;
         
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.Write(exception.ToString());
        }

    }
#endif

#if !UNITY_EDITOR
    async void requestScans(string projectName)
    {
        try {
            // request projects
            writer.WriteString("scans:" + projectName);
            await writer.StoreAsync();
            await writer.FlushAsync();
            uint size = 4;
            data = new byte[size];
            await reader.LoadAsync(size);
            reader.ReadBytes(data);
            var str = System.Text.Encoding.ASCII.GetString(data);
            uint msgSize = UInt32.Parse(str);
            System.Diagnostics.Debug.Write("MESSAGE SIZE: " + msgSize.ToString());
            writer.WriteString("scans-ready");
            await writer.StoreAsync();
            await writer.FlushAsync();

            await reader.LoadAsync(msgSize);
            data = new byte[msgSize];
            reader.ReadBytes(data);
            str = System.Text.Encoding.ASCII.GetString(data);
            string[] scans = str.Split('\n');
            // handle message
            ScanMenu.scans = scans;
            ScanMenu.scansReadyFlag = true;
            
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.Write(exception.ToString());
        }
        

    }
#endif

#if !UNITY_EDITOR
    async void requestScan(string scanName)
    {
        try {
            // request projects
            writer.WriteString("get-scan:" + scanName);
            await writer.StoreAsync();
            await writer.FlushAsync();
            uint size = 4;
            data = new byte[size];
            await reader.LoadAsync(size);
            reader.ReadBytes(data);
            var str = System.Text.Encoding.ASCII.GetString(data);
            uint msgSize = UInt32.Parse(str);
            System.Diagnostics.Debug.Write("MESSAGE SIZE: " + msgSize.ToString());
            writer.WriteString("get-scan-ready");
            await writer.StoreAsync();
            await writer.FlushAsync();

            await reader.LoadAsync(msgSize);
            data = new byte[msgSize];
            reader.ReadBytes(data);
            // handle message
            str = System.Text.Encoding.ASCII.GetString(data);
            string[] scanParse = str.Split(';');
            
            BrainMenu.dateTimeStr = scanParse[2].Split(':')[1];
            BrainMenu.scanNameStr = scanParse[0].Split(':')[1];
            BrainMenu.patientNameStr = scanParse[1].Split(':')[1];          
            string height = scanParse[3].Split(':')[1];
            string width = scanParse[4].Split(':')[1];
            string depth = scanParse[5].Split(':')[1];

            // get notes
            List<string> notes = new List<string>();
            List<Vector3> positions = new List<Vector3>();
            if (scanParse.Length > 6)
            {
                for (int i = 6; i < scanParse.Length; i++)
                {
                    if (!string.IsNullOrEmpty(scanParse[i]))
                    {
                        string[] noteParse = scanParse[i].Split(':')[1].Split('~');
                        Vector3 newPos;
                        newPos.x = float.Parse(noteParse[0]);
                        newPos.y = float.Parse(noteParse[1]);
                        newPos.z = float.Parse(noteParse[2]);
                        positions.Add(newPos);
                        notes.Add(noteParse[3]);
                    }
                }
                brain.initNotes(notes, positions);
            }

            // request pixels
            writer.WriteString("pixels");
            await writer.StoreAsync();
            await writer.FlushAsync();
            size = 8;
            data = new byte[size];
            await reader.LoadAsync(size);
            reader.ReadBytes(data);
            str = System.Text.Encoding.ASCII.GetString(data);
            msgSize = UInt32.Parse(str);
            writer.WriteString("pixels-ready");
            await writer.StoreAsync();
            await writer.FlushAsync();
            await reader.LoadAsync(msgSize);
            data = new byte[msgSize];
            reader.ReadBytes(data);
            brainLoader.data = data;
            brainLoader.width = Int32.Parse(width);
            brainLoader.height = Int32.Parse(height);
            brainLoader.depth = Int32.Parse(depth);
            BrainMenu.scanReady = true;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.Write(exception.ToString());
        }

    }
#endif

#if !UNITY_EDITOR
    async void sendScan(string scanName, string serializedScan)
    {
        try {
            // request projects
            writer.WriteString("save-scan:" + scanName + ";" + serializedScan);
            await writer.StoreAsync();
            await writer.FlushAsync();

            //update UI         

        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.Write(exception.ToString());
        }

    }
#endif

#if !UNITY_EDITOR
    async void sendDisconnect()
    {
        try {
            // request projects
            writer.WriteString("disconnect");
            await writer.StoreAsync();
            await writer.FlushAsync();

            // update UI         
            ConnectMenu.gameObject.SetActive(true);
            ProjectMenu.gameObject.SetActive(false);
            ScanMenu.gameObject.SetActive(false);
            BrainMenu.gameObject.SetActive(false);
            brainCube.gameObject.SetActive(false);
            establishedConnText.gameObject.SetActive(true);
            connectionText.gameObject.SetActive(false);
            disconnectButton.gameObject.SetActive(false);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.Write(exception.ToString());
        }

    }
#endif
}
