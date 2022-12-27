using UnityEditor;
using UnityEngine;

namespace LiveClient.Editor
{
    [CustomEditor(typeof(EntryClient))]
    public class EntryClientEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            
            base.OnInspectorGUI();
            
            var client = target as EntryClient;
            if (GUILayout.Button("接続"))
            {
                client.Connect();
            }

            if (GUILayout.Button("チャット送信"))
            {
                client.SendChat(client.chatInputField);
            }
            // GUILayout.Label(client.chatField);
        }
    }
}