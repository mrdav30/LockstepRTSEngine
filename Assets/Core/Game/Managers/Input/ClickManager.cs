using UnityEngine;

public static class ClickManager
{
    private static float[] keyEPress = new float[0];
    private static KeyCode[] keys = new KeyCode[0];
    private static float keyPressTimeout = 0.5f; //The time (in seconds) to press the same key again to make it count as double click.

    public static bool DoubleClick(KeyCode key)
    {
        bool result = false, revisedKey = false;
        int availableKeys = 0;
        //Removes Forgotten Keys from Array
        for (int forg = 0; forg < keys.Length; forg++)
        {
            //If its been a while scince last key press
            if (keyEPress[forg] < Time.time) { keys[forg] = KeyCode.None; keys[forg] = 0; } else { availableKeys++; }
        }
        //Funnels array values into free spaces - Removes "KeyCode.None" elements
        KeyCode[] nKeys = new KeyCode[availableKeys]; float[] nKeyEPress = new float[availableKeys];
        for (int funn = 0; funn < keys.Length; funn++)
        {
            //If the current key is not "null"
            if (keys[funn] != KeyCode.None)
            {
                //Looks at all nKey elements
                for (int fum = 0; fum < availableKeys; fum++)
                {
                    //If the element is "null"
                    if (nKeys[fum] == KeyCode.None)
                    {
                        //This key claims the element
                        nKeys[fum] = keys[funn];
                        nKeyEPress[fum] = keyEPress[funn];
                    }
                }
            }
        }
        //Arrays are updated.
        keys = nKeys;
        keyEPress = nKeyEPress;
        //Checks if the key is already assigned to a element in the array
        for (int rev = 0; rev < keys.Length; rev++) { if (keys[rev] == key) { revisedKey = true; rev = keys.Length; } }
        //If it is a brand new key
        if (!revisedKey)
        {
            //Creates a new arrays
            KeyCode[] newKeys = new KeyCode[keys.Length + 1];
            float[] newKeyEPress = new float[keys.Length + 1];
            //Copies the arrays to them
            keys.CopyTo(newKeys, 0);
            keyEPress.CopyTo(newKeyEPress, 0);
            //Assigns current key to the very last element
            newKeys[keys.Length] = key;
            newKeyEPress[keys.Length] = Time.time + keyPressTimeout; //The current time plus keyPressTimeout - time to double-click
                                                                     //Updates arrays
            keys = newKeys;
            keyEPress = newKeyEPress;

            //However, if the key is still in the Keys array (It was pressed within 0.5 seconds)
        }
        else
        {
            //The key is now "ignored", thus next press, even if it would be within the original 0.5 seconds, will be counted as original press
            for (int ign = 0; ign < keys.Length; ign++) { if (keys[ign] == key) { keyEPress[ign] = 0; ign = keys.Length; } }
            result = true; //Returns ture for Double Press
        }

        return result;
    }
}