using System.Collections;
using System.Collections.Generic;
using Timers;
using UnityEngine;

public class ChainedTimers : MonoBehaviour
{
    void Start()
    {
        TimersManager.SetTimer(this, 1f, () =>
        {
            Debug.Log("Timer 1");
            TimersManager.SetTimer(this, 1f, () =>
            {
                Debug.Log("Timer 2");
                TimersManager.SetTimer(this, 1f, () =>
                {
                    Debug.Log("Timer 3");
                    TimersManager.SetTimer(this, 1f, () =>
                    {
                        Debug.Log("Timer 4");
                        TimersManager.SetTimer(this, 1f, () =>
                        {
                            Debug.Log("Timer 5");
                        });
                    });
                });
            });
        });
    }
}
