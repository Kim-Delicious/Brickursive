using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlasticPipe.PlasticProtocol.Messages;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static GameConfig;


public class BlackboxBehavior : MonoBehaviour
{    public enum StructureType
    {
        /// <summary>
        /// This Structure takes one of the child bricks, and instantiates another.
        /// </summary>
        Replicator,

        Belt,

        Vacuum,

        Joiner

        
    }


    public StructureType structureType;

    /// <summary>
    /// This is not in real-time seconds. But scaled based on the target animation FPS of 12. 
    /// Therefore 12n -> n seconds.
    /// </summary>
    public int spawnTime = 1;

    public int powerLevel = 0;

    private int[] inventory;


    [SerializeField]
    private List<GameObject> detectedBricks;

    private BrickLibrary brickLibrary;



    void Start()
    {
        detectedBricks = new();

        brickLibrary = GameObject.Find("Brick Library").GetComponent<BrickLibrary>();
        inventory = new int[brickLibrary.allBricks.Count];

       // Debug.Log("Inventory Length: " + inventory.Length);

      
    }

    void FixedUpdate()
    {

        if(powerLevel > 0)
        {
            //In Fixed update it is delayed already. Make sure to consider this .
            Invoke(nameof(RunBehaviorByStructureType), spawnTime * ANIMATION_UPDATE_TIME);
        }



    }



    private void RunBehaviorByStructureType()
    {

        switch (structureType)
        {
            case StructureType.Replicator:
                RunReplicatorBehavior();
                break;
            
            case StructureType.Belt:
                RunBeltBehavior();
                break;

            case StructureType.Vacuum:
                RunVacuumBehavior();
                break;

            case StructureType.Joiner:
                RunJoinerBehavior();
                break;

            default:
                break;

        };

        CancelInvoke();
    }


    private void RunReplicatorBehavior()
    {
        Transform childToReplicate = GetFirstValidChildTransformInHeirarchy();

        if(childToReplicate == null)
        {
            return;
        }

        Vector3 spawnPos = transform.position + Vector3.Scale(transform.forward, GetComponent<BrickBehavior>().trueScale );

        GameObject newBrick = Instantiate(childToReplicate.gameObject, spawnPos, transform.rotation, GameObject.Find(OBJECT_FOLDER_NAME).transform);

        Rigidbody newBrickRB = newBrick.GetComponent<Rigidbody>();

        newBrickRB.isKinematic = false;
        newBrickRB.useGravity = true;
        newBrickRB.excludeLayers = 0;
        //newBrickRB.AddForce(transform.forward * 3, ForceMode.VelocityChange);
        //newBrickRB.AddTorque(Vector3.one, ForceMode.Impulse);



    }

    private void RunBeltBehavior()
    {
        SetupBelt();

        if(detectedBricks.Count <= 0)
        {
            return;
        }

        for(int i = 0; i < detectedBricks.Count; i++)
        {
            GameObject brick = detectedBricks[i];

            if(brick == null)
            {
                Debug.Log("A null object is in the list");
                detectedBricks.Remove(brick);
                continue;

            }

            Rigidbody brickRb = brick.GetComponent<Rigidbody>();

            


            if(brick.GetComponent<BlackboxBehavior>() == null)
            {
                brickRb.AddForce(Vector3.Scale(transform.up, -brickRb.velocity), ForceMode.VelocityChange);
                brickRb.rotation = transform.rotation;

            }

            

            /*Vector3 newPos = transform.position;
            newPos.x = brick.transform.position.x;
            newPos.z = brick.transform.position.z;
            Vector3 scaleThing = Vector3.Scale(GetComponent<BrickBehavior>().trueScale, -transform.up);
            Vector3 targetScaleThing = Vector3.Scale(brick.GetComponent<BrickBehavior>().trueScale, -transform.up);
            newPos.y += scaleThing.y / 2;
            newPos.y += targetScaleThing.y / 2;

            Debug.Log(transform.up);



            brick.transform.position = newPos;*/

            //Alter Move direction so that a brick aligns itself with the belt as it moves.

            Vector3 moveDirection = Quaternion.Inverse(brick.transform.rotation) * transform.forward;

            brick.GetComponent<BrickBehavior>().TranslateBrick(gameObject, 
            moveDirection * 100f * BASE_CELL_SIZE.y  * Time.deltaTime);


        }





    }


    private void PrimeBrickForMovement(Rigidbody brickRb)
    {

        brickRb.useGravity = false;
        brickRb.constraints = RigidbodyConstraints.FreezeAll;

        //AlignWithBelt(brickRb);

    }

    private void SetupBelt()
    {
        if(GetComponents<Collider>().Length <= 1)
        {
            Vector3 trueScale = GetComponent<BrickBehavior>().trueScale;

            BoxCollider detector = gameObject.AddComponent<BoxCollider>();

            detector.center = new Vector3(0f, trueScale.y / 2f, 0f);
            detector.size = new Vector3(trueScale.x - (STUD_HEIGHT * 2),
                                        trueScale.y - (STUD_HEIGHT * 2),
                                        trueScale.z - (STUD_HEIGHT * 2));

            detector.isTrigger = true;
        } 


    }

    private void RunVacuumBehavior()
    {
        if(detectedBricks.Count <= 0)
        {
            return;
        }


        for(int i = 0; i < detectedBricks.Count; i++)
        {
            GameObject brick = detectedBricks[i];

            detectedBricks.Remove(brick);
            Destroy(brick);

            //Debug.Log("Sucked up a " + brick.name);


        }


    }

    private void RunJoinerBehavior()
    {
        int yDiff = 0;
        int xDiff = 0;

        //Quaternion parentInverse = Quaternion.Inverse(GetComponent<BrickBehavior>().highestParent.rotation);
        Quaternion parentInverse = Quaternion.Inverse(GameObject.Find(OBJECT_FOLDER_NAME).transform.rotation);
        
        Quaternion diff = parentInverse * transform.rotation;

        Vector3 diffEuler = diff.eulerAngles;

        // 90 or 270
        if (diffEuler.y >= 45 && 
            diffEuler.y < 135 || 
            diffEuler.y >= 225 && 
            diffEuler.y < 315)
        {
            yDiff = 1;
        }

        if (diffEuler.x >= 45 && 
            diffEuler.x < 135 || 
            diffEuler.x >= 225 &&
            diffEuler.x < 315)
        {
            xDiff = 1;
        }

        //Vector3 spawnPos = transform.position + Vector3.Scale(-transform.forward, GetComponent<BrickBehavior>().trueScale );
        Vector3 spawnPos = transform.position - (transform.forward * 2);


        if(xDiff == 1)
        {
            JoinBricksByYAxis(spawnPos);
            return;
        }

        if(yDiff == 1)
        {
            JoinBricksByZAxis(spawnPos);
            return;
        }

        JoinBricksByXAxis(spawnPos);

        
      


    }

    private void JoinBricksByXAxis(Vector3 spawnPos)
    {

        if(inventory[1] >= 3)
        {//1x1 brick
            Instantiate(brickLibrary.allBricks[2], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            inventory[1] -= 3;
            return;
        }
        if(inventory[2] >= 4)
        {//1x4
            Instantiate(brickLibrary.allBricks[3], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            inventory[2] -= 4;
            return;
        }
        Debug.Log(inventory[3]);
        if(inventory[3] >= 2)
        {//2x2 brick
            Instantiate(brickLibrary.allBricks[5], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            inventory[3] -= 2;
            return;
        }


    }

    private void JoinBricksByYAxis(Vector3 spawnPos)
    {
        if(inventory[1] >= 1)
        {//1x1 Stud

            Instantiate(brickLibrary.allBricks[0], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            inventory[1] -= 1;
            return;
        }
        /*if(inventory[2] >= 4)
        {//4x1 pillar

            GameObject newBrick = Instantiate(brickLibrary.allBricks[4], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            Vector3 scale = newBrick.GetComponent<BrickBehavior>().trueScale;          
            newBrick.transform.position += Vector3.Scale(-transform.forward, scale);
            inventory[2] -= 4;
            return;
        }*/

        if(AddArrayToItself(inventory) >= 25)
        {//16x16

            Instantiate(brickLibrary.allBricks[6], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            inventory = new int[inventory.Length];
            return;
        }
        
    }

    private void JoinBricksByZAxis(Vector3 spawnPos)
    {
        if(inventory[0] >= 4)
        {//2x2 Universal Female 

            Instantiate(brickLibrary.allBricks[7], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            inventory[0] -= 4;
            return;
        }
        if(inventory[1] >= 4)
        {//2x2 Universal Male 

            Instantiate(brickLibrary.allBricks[8], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            inventory[1] -= 4;
            return;
        }

        if(inventory[2] >= 1)
        {//1x1 Panel

            Instantiate(brickLibrary.allBricks[1], spawnPos, Quaternion.identity, GameObject.Find(OBJECT_FOLDER_NAME).transform);
            inventory[2] -= 1;
            return;
        }

        
    }

    private int AddArrayToItself(int[] array)
    {
        int total = 0;
        for (int i = 0; i < array.Length; i++)
        {
            total += array[i];
        }

        return total;

    }

    private void AddBrickToInventory(GameObject brick)
    {

        for(int i = 0; i < brickLibrary.allBricks.Count; i++)
        {
            string libBrickName = brickLibrary.allBricks[i].name;
            libBrickName += "(Clone)";

            string brickName = brick.name;
            if(brickName == libBrickName)
            {
                inventory[i] += 1;
                Destroy(brick);
                return;
            }

        }

        Debug.Log("Triggering Object not found in Brick Library");
        
    }
    private Transform GetFirstValidChildTransformInHeirarchy()
    {
        Transform chosenChild = null;
        for(int i = 0; i < transform.childCount; i++)
        {

            Transform child = transform.GetChild(i);

            if(child.CompareTag(SOCKET_TAG_FEMALE) || child.CompareTag(SOCKET_TAG_MALE))
            {
                continue;
            }

            if(child.GetComponent<XRGrabInteractable>() == null)
            {
                continue;
            }

            chosenChild = child;



        }

        return chosenChild;

    }

    private bool IsAvailableDetector()
    {
        bool isAvailable = false;
        for(int i = 0; i < transform.childCount; i++)
        {

            Transform child = transform.GetChild(i);

            if(child.name == "Detector")
            {
                isAvailable = true;
                break;
            }
        }


        return isAvailable;

    }



    void OnTriggerEnter(Collider collider)
    {
        
        if(collider.isTrigger)
        {
            return;
        }

        GameObject hitBrick = collider.gameObject;
        
        
        if(!hitBrick.CompareTag(BASE_BRICK_TAG))
        {
            return;
        }

        if(hitBrick.GetComponent<BrickBehavior>().highestParent == GetComponent<BrickBehavior>().highestParent)
        {
            return;
        }

        if(hitBrick.transform.parent != null &&
           hitBrick.transform.parent.gameObject == gameObject)
        {
            return;
        }

        if(transform.parent != null &&
          hitBrick == transform.parent.gameObject)
        {
            return;
        }

        
        


        for (int i = 0; i < detectedBricks.Count; i++)
        {
            if(hitBrick == detectedBricks[i])
            {
                return;
            }  
        }

        if(structureType == StructureType.Joiner)
        {
            AddBrickToInventory(hitBrick);
            return; 
        }





        detectedBricks.Add(hitBrick);

        if(structureType == StructureType.Belt)
        {
            PrimeBrickForMovement(hitBrick.GetComponent<Rigidbody>());
            hitBrick.GetComponent<BrickBehavior>().belts.Add(gameObject);
        }

        

        
        
    }

    void OnTriggerExit(Collider collider)
    {


        GameObject hitBrick = collider.gameObject;
        if(!collider.CompareTag(BASE_BRICK_TAG))
        {
            return;
        }

        if(hitBrick.GetComponent<BrickBehavior>().highestParent == GetComponent<BrickBehavior>().highestParent)
        {
            return;
        }

        detectedBricks.Remove(hitBrick);

        if(structureType == StructureType.Belt)
        {
            hitBrick.GetComponent<BrickBehavior>().belts.Remove(gameObject);

            if(hitBrick.GetComponent<BrickBehavior>().belts.Count <= 0)
            {

                Rigidbody brickRb = hitBrick.GetComponent<Rigidbody>();

                brickRb.useGravity = true;
                brickRb.constraints = RigidbodyConstraints.None;
                //brickRb.AddForce(transform.forward * 15f, ForceMode.Impulse);
            }
        }

        


    }



}
