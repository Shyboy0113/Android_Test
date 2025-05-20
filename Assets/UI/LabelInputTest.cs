using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelInputTest : MonoBehaviour
{
#if UNITY_EDITOR
    public string textTest = "";
    public int typeTest = 0;

    string testString = "012";
    void Start()
    {
        StartCoroutine(TestLabelInput());
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            GetComponent<UIManager>().RequestLabel(textTest, (LabelType)(testString[typeTest] - '0'));
            Debug.Log(textTest);
        }
    }

    IEnumerator TestLabelInput() 
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            GetComponent<UIManager>().RequestLabel("label test", 0);
            Debug.Log("label testing...");
        }
    }
#endif
}
