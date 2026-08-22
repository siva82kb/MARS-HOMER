using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class trailnumber : MonoBehaviour
{
    public TextMeshProUGUI trialNo;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        trialNo.text = $"{AppData.Instance.selectedMovement.trialNumberDay}/ {AppData.Instance.userData.moveTimePrsc[AppData.Instance.selectedMovement.name]}";
    }
}
