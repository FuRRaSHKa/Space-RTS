using HalloGames.Architecture.Exceptions;
using HalloGames.Architecture.Saves.Data;
using System;

namespace HalloGames.Architecture.Saves
{
    public abstract class BaseSave
    {
        internal string SaveName { get; }

        internal abstract Type SaveType { get; }

        public BaseSave(string saveName)
        {
            SaveName = saveName;
        }

        internal abstract void ApplyData(SaveData saveData);
        internal abstract SaveData CreateAndApplyDefaultSave();
        internal abstract SaveData GetSaveData();

        internal void Start()
        {
            OnStart();
        }

        internal void End()
        {
            OnEnd();
        }

        protected virtual void OnStart() 
        {
        
        }
        
        protected virtual void OnEnd() 
        {
        
        }
    }

    public abstract class BaseSave<TData> : BaseSave where TData : SaveData
    {
        internal override Type SaveType => typeof(TData);
        protected TData Data { get; private set; }

        protected BaseSave(string saveName) : base(saveName)
        {
        }

        internal override void ApplyData(SaveData saveData)
        {
            if(saveData is TData data)
                Data = data;
            else
                throw new TypeMismatchException(SaveType, saveData.GetType());

            OnDataSet();
        }

        internal override SaveData CreateAndApplyDefaultSave()
        {
            Data = CreateDefaultSave();
            OnDataSet();

            return Data;
        }
        protected abstract TData CreateDefaultSave();

        internal override SaveData GetSaveData()
        {
            return Data;
        }

        internal virtual void OnDataSet() 
        {

        }
    }
}
