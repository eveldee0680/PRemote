﻿using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;

using MessagePack;
using PRemote.Shared;

namespace PRemote.Client.Android
{
    /// <summary>
    /// Main Activity, all is in it
    /// </summary>
    [Activity(Label = "PRemote", MainLauncher = true, Theme = "@android:style/Theme.Material")]
    public class MainActivity : Activity
    {
        TextView txt_State;
        ImageView img_State;
        Spinner spr_ISO;
        Spinner spr_Aperture;
        Spinner spr_Shutter;
        Button btn_Picture;
        Button btn_Vocal;

        IPEndPoint _serverIp;
        PacketStream _packetStream;
        CameraCapabilities _capabilities;
        bool _connected = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Register View
            txt_State = FindViewById<TextView>(Resource.Id.txt_State);
            img_State = FindViewById<ImageView>(Resource.Id.img_State);
            spr_ISO = FindViewById<Spinner>(Resource.Id.spr_ISO);
            spr_Aperture = FindViewById<Spinner>(Resource.Id.spr_Aperture);
            spr_Shutter = FindViewById<Spinner>(Resource.Id.spr_Shutter);
            btn_Picture = FindViewById<Button>(Resource.Id.btn_Picture);
            btn_Vocal = FindViewById<Button>(Resource.Id.btn_Vocal);

            spr_ISO.ItemSelected += Spr_Change;
            spr_Aperture.ItemSelected += Spr_Change;
            spr_Shutter.ItemSelected += Spr_Change;

            btn_Picture.Click += Btn_Picture_Click;
            btn_Vocal.Click += Btn_Vocal_Click;

            UDP_Thread();
        }

        private async void Spr_Change(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (_connected)
            {
                await _packetStream.SendAsync(new PPacket(PDataType.ISO, (int)spr_ISO.SelectedItem));
                await _packetStream.SendAsync(new PPacket(PDataType.Aperture, (double)spr_Aperture.SelectedItem));
                await _packetStream.SendAsync(new PPacket(PDataType.ShutterSpeed, (string)spr_Shutter.SelectedItem));
            }
        }

        private void Btn_Vocal_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        // Take a picture
        private async void Btn_Picture_Click(object sender, EventArgs e)
        {
            if (_connected)
            {
                await _packetStream.SendAsync(new PPacket(PDataType.Picture, 0));
            }
        }

        // Receive UDP broadcast to get discovered IP
        private void UDP_Thread()
        {
            // Set up a new client
            var udpClient = new UdpClient(PConnection.UDPPort)
            {
                EnableBroadcast = true
            };

            var ip = new IPEndPoint(IPAddress.Any, PConnection.UDPPort);

            // Check broadcast packets
            do
            {
                byte[] data = udpClient.Receive(ref ip);

                if (data.SequenceEqual(PConnection.UDPPacketData))
                    _serverIp = ip;

            } while (_serverIp == null);

            // Set state:
            txt_State.Text = $"Connecté: {_serverIp.ToString()}";

            //Tcp connection
            TCP_Connection();
        }

        // Connect to server
        private void TCP_Connection()
        {
            // Connect to server
            var tcpClient = new TcpClient(_serverIp.Address.ToString(), PConnection.TCPPort);

            // Get the stream
            _packetStream = new PacketStream(tcpClient.GetStream());

            // Receive and display capabilities
            _capabilities = _packetStream.Receive<CameraCapabilities>();

            spr_ISO.Adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleListItem1, _capabilities.SupportedIsoSpeeds);
            spr_Aperture.Adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleListItem1, _capabilities.SupportedApertures);
            spr_Shutter.Adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleListItem1, _capabilities.SupportedShutterSpeeds);

            _connected = true;
        }
    }
}

