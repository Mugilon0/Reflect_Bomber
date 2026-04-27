using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// IConnectionTokenProvider.cs
public interface IConnectionTokenProvider
{
    byte[] GetConnectionToken();
}