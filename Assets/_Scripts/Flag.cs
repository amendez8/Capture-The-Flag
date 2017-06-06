using UnityEngine;
using System.Collections;

public class Flag : MonoBehaviour {

    private Rigidbody rb;

	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnCollisionEnter(Collision collision)
    {
        AI1 aiCharacter = collision.gameObject.GetComponent<AI1>();
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();

        if(aiCharacter && aiCharacter.GetTarget() == true && gameObject.tag == "F1") // if flag 1 hit by ai, assign flag to ai character.
        {
            transform.parent = aiCharacter.transform;
            transform.position = new Vector3(aiCharacter.transform.position.x, aiCharacter.transform.position.y, aiCharacter.transform.position.z);
            //Object.Destroy(GetComponent<Rigidbody>());
        }

        if (enemy && enemy.GetTarget() == true && gameObject.tag == "F2") // if flag 2 hit by enemy, assign flag to enemy character.
        {
            transform.parent = enemy.transform;
            transform.position = new Vector3(enemy.transform.position.x, enemy.transform.position.y, enemy.transform.position.z + 0.5f);
        }
    }

    public void returnToHomeSpot()
    {
        if (gameObject.tag == "F1")
        {
            transform.position = new Vector3(255, 0, 533); // assign flag1 back to original spot
            transform.parent = null;
            rb.detectCollisions = true;
           // gameObject.AddComponent<Rigidbody>();
        }
        if (gameObject.tag == "F2")
        {
            transform.position = new Vector3(242.0f, -0.5f, 28.0f); // assign flag2 back to original spot
            transform.parent = null;
           // gameObject.AddComponent<Rigidbody>();
        }
    }
}
