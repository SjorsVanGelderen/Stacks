/*
    The visitor design pattern prevents
    null reference exceptions at compile time
*/

using System;

public interface Option<T>
{
    U Visit<U>(Func<T, U> _onSome, Func<U> _onNone);
}

public sealed class Unit
{
    private static readonly Unit instance = new Unit();
    
    private Unit()
    {
        
    }
    
    public static Unit Instance
    {
        get
        {
            return instance;
        }
    }
}

public class Some<T> : Option<T>
{
    T value;
    
    public Some(T _value)
    {
        this.value = _value;
    }
    
    public U Visit<U>(Func<T, U> _onSome, Func<U> _onNone)
    {
        return _onSome(value);
    }
}

public class None<T> : Option<T>
{
    public U Visit<U>(Func<T, U> _onSome, Func<U> _onNone)
    {
        return _onNone();
    }
}