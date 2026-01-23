using UnityEngine;

public class ShowBasedOnInput : MonoBehaviour
{
    public InputType ShowForInputType;
}

public enum InputType
{
    Any,
    MouseOnly,
    NoMouse
}