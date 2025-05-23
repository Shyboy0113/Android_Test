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
#if UNITY_EDITOR
        StartCoroutine(TestLabelInput());
#endif
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
            yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
            GetComponent<UIManager>().RequestLabel("label test" + Random.Range(0, 20), LabelType.Warning);
            Debug.Log("label testing...");
        }
    }
#endif
}
