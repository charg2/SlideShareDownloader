﻿namespace Mala.Core;

public class Singleton< T > where T : Singleton< T >, new()
{
    private static readonly Lazy< T > _instance = new Lazy< T >( () => new T() );

    public static T Instance => _instance.Value;
}
