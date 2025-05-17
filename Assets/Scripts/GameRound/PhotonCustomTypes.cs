using ExitGames.Client.Photon;
using Photon.Realtime;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class PhotonCustomTypes
{
    public static void Register()
    {
        PhotonPeer.RegisterType(
            typeof(PlayerData),
            (byte)'P',
            SerializePlayerData,
            DeserializePlayerData
        );
    }

    private static short SerializePlayerData(StreamBuffer outStream, object customObject)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            bf.Serialize(ms, customObject);
            byte[] data = ms.ToArray();
            outStream.Write(data, 0, data.Length);
            return (short)data.Length;
        }
    }

    private static object DeserializePlayerData(StreamBuffer inStream, short length)
    {
        byte[] data = new byte[length];
        inStream.Read(data, 0, length);
        using (MemoryStream ms = new MemoryStream(data))
        {
            BinaryFormatter bf = new BinaryFormatter();
            return (PlayerData)bf.Deserialize(ms);
        }
    }
}
