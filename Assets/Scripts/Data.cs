using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Data : MonoBehaviour
{
    public static void SaveProfile(ProfileData t_profile)
    {
        try
        {
            //Pretty much tries to make a custom file on the PC that stores the player profile details such as name and such.
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path)) File.Delete(path);

            FileStream file = File.Create(path);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, t_profile);
            file.Close();

            //Debug.Log("SAVED SUCCESSFULY!");
        }

        catch
        {
            Debug.Log("SOMETHING WENT TERRIBLY WRONG");
        }
    }

    public static ProfileData LoadProfile()
    {
        ProfileData ret = new ProfileData();

        try
        {
            //This pretty much also loads the file from the path we made.
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path))
            {
                FileStream file = File.Open(path, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                ret = (ProfileData)bf.Deserialize(file);

                //Debug.Log("LOADED SUCCESSFULY!");
            }
        }

        catch
        {
            Debug.Log("FILE WAS NOT FOUND!");
        }
        return ret;
    }
}
