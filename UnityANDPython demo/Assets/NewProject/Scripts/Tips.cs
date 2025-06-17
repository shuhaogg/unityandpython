using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// øøΩ¸œ‘ æ±Í«©
/// </summary>
public class Tips : MonoBehaviour
{

    public GameObject description;
    private void Start()
    {
        if (description == null)
            return;
        description.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (description == null)
            return;
        description.SetActive(true);
    }
    private void OnTriggerExit(Collider other)
    {
        if (description == null)
            return;
        description.SetActive(false);

    }
}


