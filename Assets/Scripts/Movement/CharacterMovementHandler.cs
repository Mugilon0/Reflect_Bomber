using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class CharacterMovementHandler : NetworkBehaviour
{

    //other components
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;


    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData networkInputData)) //　Get the input from the network
        {
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
            moveDirection.Normalize();

            networkCharacterControllerPrototypeCustom.Move(moveDirection); // データはおそらくCharacterInputHandlerからもらう
        }
    }



}
