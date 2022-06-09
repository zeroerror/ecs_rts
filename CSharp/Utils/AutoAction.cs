using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoAction : MonoBehaviour
{
    public float delay = 1.0f;
    private float _delay;
    private bool isDone = false;
    public enum Action
    {
        Disable,
        Destroy
    }
    public Action action;

    private void Start()
    {
        _delay = delay;
    }
    private void Update()
    {
        if (delay > 0) delay -= Time.deltaTime;
        else if (!isDone)
        {
            switch (action)
            {
                case Action.Disable:
                    this.gameObject.SetActive(false);
                    isDone = true;
                    break;
                case Action.Destroy:
                    GameObject.Destroy(this.gameObject);
                    isDone = true;
                    break;
                default:
                    break;
            }
        }
        else
        {
            switch (action)
            {
                case Action.Disable:
                    if (gameObject.activeSelf)
                    {
                        isDone = false;
                        delay = _delay;
                    }
                    break;
                case Action.Destroy:
                    break;
                default:
                    break;
            }
        }
    }
}
