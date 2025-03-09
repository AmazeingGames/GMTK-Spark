using NUnit.Framework;
using UnityEngine;

public class AssertOriginPosition : MonoBehaviour
{
    [SerializeField] string errorMessage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.LogWarning("Convert error message into an enum and a dictionary to write the error message saved in the script, rather than in the inspector");
        Assert.That((Vector2)transform.position, Is.EqualTo(Vector2.zero), errorMessage);   
    }
}
