using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// 
/// </summary>
public class AssertOriginPosition : MonoBehaviour
{
    [SerializeField] string errorMessage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.LogWarning("Convert error message into an enum and a dictionary to write the error message saved in the script, rather than in the inspector");
        Assert.AreEqual((Vector2)transform.position, Vector2.zero, errorMessage);
    }
}
