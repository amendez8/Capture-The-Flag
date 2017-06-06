using UnityEngine;
using System.Collections;

public class Controller : MonoBehaviour {

    int aiDecider;
    int enemyDecider;

    void Start () {
        aiDecider = Random.Range(1, 3);
        enemyDecider = Random.Range(1, 3);
       // Debug.Log("number: " + aiDecider);
    }
	
	void Update () {
        
    }
    public int GetAIDecider()
    {
        return aiDecider;
    }

    public int GetEnemyDecider()
    {
        return enemyDecider;
    }
}
