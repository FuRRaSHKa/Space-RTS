using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAbstractFactory<TObject, TData> 
    where TObject : class
    where TData : struct
{
    public TObject CreateObject(in TData data);
}