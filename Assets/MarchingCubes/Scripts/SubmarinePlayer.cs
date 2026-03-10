using UnityEngine;

public class SubmarinePlayer : MonoBehaviour {
    [SerializeField] private float forwardSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float propellerSpeed;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject propeller;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        propeller.transform.Rotate(propellerSpeed * Time.deltaTime * player.transform.up, Space.World);
        transform.Translate(-forwardSpeed * Time.deltaTime * player.transform.up, Space.World);
        if (Input.GetKey(KeyCode.UpArrow)) {
            player.transform.Rotate(turnSpeed * Time.deltaTime * transform.right);
        }
        if (Input.GetKey(KeyCode.DownArrow)) {
            player.transform.Rotate(-turnSpeed * Time.deltaTime * transform.right);
        }
        if (Input.GetKey(KeyCode.LeftArrow)) {
            transform.Rotate(-turnSpeed * Time.deltaTime * Vector3.up, Space.World);
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            transform.Rotate(turnSpeed * Time.deltaTime * Vector3.up, Space.World);
        }
    }
    
    
}
