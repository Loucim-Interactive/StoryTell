using System;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    [Serializable] public sealed class ComfortValues
    {
        public float sensitivity=1, textSize=1, motion=1, darkness=1, master=1, voices=1, effects=1;
        public void Validate()
        {
            sensitivity=Clamp(sensitivity,.2f,3);textSize=Clamp(textSize,.85f,1.25f);
            motion=Clamp(motion,0,1);darkness=Clamp(darkness,0,1);
            master=Clamp(master,0,1);voices=Clamp(voices,0,1);effects=Clamp(effects,0,1);
        }
        private static float Clamp(float value,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?1:Mathf.Clamp(value,min,max);
    }
    public static class ComfortSettings
    {
        private const string Key="StoryTell.Comfort.v1";
        public static ComfortValues Current { get; private set; }=new();
        public static event Action Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Changed=null;Current=new ComfortValues();
            if(Application.isBatchMode)return;
            try{Current=JsonUtility.FromJson<ComfortValues>(PlayerPrefs.GetString(Key,JsonUtility.ToJson(Current)))??new ComfortValues();}
            catch(ArgumentException){Current=new ComfortValues();}
            Current.Validate();
        }
        public static void Apply(ComfortValues values,bool persist=true)
        {
            values.Validate();Current=values;
            if(persist&&!Application.isBatchMode){PlayerPrefs.SetString(Key,JsonUtility.ToJson(values));PlayerPrefs.Save();}
            Changed?.Invoke();
        }
    }
}
