using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiveClient;
using LiveCoreLibrary.Client;
using LiveCoreLibrary.Commands;
using LiveCoreLibrary.View;
using UnityEngine;

public class ColorView : TcpView<ColorPacket>
{
    [SerializeField] private Entry entry;

    [SerializeField] private GameObject self;
    [SerializeField] private Color color;

    [SerializeField] private UnityEngine.UI.Button button;

    private void Start()
    {
        button.onClick.AddListener(() =>
        {
            if (LiveNetwork.Instance.IsConnected)
            {
                var command = new ColorPacket(entry.selfUser.Id, color.r, color.g, color.b);
                SendTcp(command);
            }

            self.GetComponent<MeshRenderer>().material.color = color;
          
        });
    }

    protected override void OnPacketReceived(ColorPacket colorPacket)
    {
        if (colorPacket.Id == entry.selfUser.Id) return;
        
        if (entry._userHolder.TryGetValue(colorPacket.Id, out var user) && user.Go)
        {
            user.Go.GetComponent<MeshRenderer>().material.color= new Color(colorPacket.R, colorPacket.G, colorPacket.B);
        }
    }
}
