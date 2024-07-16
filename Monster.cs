using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
public class Monster : MonoBehaviour
{
    public float speed = 5f;
    public NavMeshAgent agent;
    public GameObject player;
    public GameObject monsterTeleportPositions;
    public GameObject monsterWalkDuos;
    private GameObject waypoint;
    public GameOver gameOver;
    public AudioSource playAudio;
    private bool followingPlayer = true;
    private float fadeAway = 0.6f;
    public bool isGameOver = false;
    private int state = 0; //state 0 = going from waypoint to waypoint. state = 1 go towards player. 2 = idle
    public Animator monsterAnimator;
    // Start is called before the first frame update
    void Start()
    {
        agent.SetDestination(player.transform.position);
        agent.speed = speed;
        StartFollowing();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (followingPlayer){
            monsterAnimator.SetBool("Walking",true);
            if(state == 1){
                agent.SetDestination(player.transform.position);
                agent.speed = speed;
            }else if (state == 0){ 
                agent.SetDestination(waypoint.transform.position);
                agent.speed = speed;
                
                if ((waypoint.transform.position - transform.position).magnitude<4){
                    Debug.Log("Reached waypoint");
                    SetFollowing(false);
                }
            }
            
        }else{
            monsterAnimator.SetBool("Walking",false);
        }
        if((player.transform.position - transform.position).magnitude<1 && !isGameOver){
            Debug.Log("You are dead.");
            
            playAudio.Play();
            StartCoroutine(GameObject.Find("GameAnalytics").GetComponent<GameAnalytics>().AddDeath(player.transform.position));
            StartCoroutine(gameOver.FadeIn());
            agent.isStopped = true; 
            player.GetComponent<PlayerController>().canMove = false;
            player.GetComponent<PlayerController>().GameOverLook(transform.gameObject);
            UnityEngine.Cursor.lockState = CursorLockMode.Confined;
            isGameOver = true;

            
        }
        
    }
    public void SetFollowing(bool following){
        if(!following && followingPlayer){
            Debug.Log("Got him");
            followingPlayer = false;
            gameObject.GetComponent<NavMeshAgent>().isStopped = true;
            StartCoroutine(FadeMonster());
            
        }
          
    }
    private void FadeChildren(Transform root) {
        for (int i = 0; i < root.childCount; i++){
            //do the thing
            if (root.GetChild(i).GetComponent<SkinnedMeshRenderer>()){
                StartCoroutine(FadeRenderer(root.GetChild(i).GetComponent<SkinnedMeshRenderer>()));
            }
            
            FadeChildren(root.GetChild(i));
        }
    }
    List<Transform> ShuffleList(List<Transform> lst){
        int n = lst.Count;
        List<Transform> newLst = new List<Transform>();
        while (n > 0)
        { 
            int randomIdx = Random.Range(0,n-1);
            newLst.Add(lst[randomIdx]);
            lst.RemoveAt(randomIdx);
            n--;
        }
        return  newLst;
    }
    void StartFollowing(){
        if (state == 0 ){
            Debug.Log("starting to follow player");
            state = 1;
            List<Transform> teleportPositions = new List<Transform>();
            foreach (Transform child in monsterTeleportPositions.transform)
            {
                teleportPositions.Add(child);
            }
            teleportPositions = ShuffleList(teleportPositions);
            Vector3 teleportPosition = teleportPositions[0].position;
            foreach (Transform child in teleportPositions)
            {
                MonsterPosition monsterPosition = child.GetComponent<MonsterPosition>();
                if (monsterPosition.doorToBeOpenForTeleport== null){
                    teleportPosition = child.position;
                    break;
                    
                }else if (monsterPosition.doorToBeOpenForTeleport.current_state != "Door closed"){
                    teleportPosition = child.position;
                    break;
                }
            }
            gameObject.GetComponent<NavMeshAgent>().Warp(teleportPosition);
        }
        else if (state == 1){ // chooses position duo where both doors are open or if there are no doors in between
            Debug.Log("Starting waypoint search");
            state = 2;
            List<Transform> waypointsDuos = new List<Transform>();
            foreach (Transform child in monsterWalkDuos.transform)
            {

                waypointsDuos.Add(child);
            }
            waypointsDuos = ShuffleList(waypointsDuos);
            waypointsDuos = ShuffleList(waypointsDuos);
            waypointsDuos = ShuffleList(waypointsDuos);
            foreach (Transform child in waypointsDuos)
            {
                bool bothDoorsUnlocked = true; 
                MonsterPosition monsterPosition1 = child.GetChild(0).GetComponent<MonsterPosition>();
                MonsterPosition monsterPosition2 = child.GetChild(1).GetComponent<MonsterPosition>();
                if (monsterPosition1.doorToBeOpenForTeleport!= null){

                    if (monsterPosition1.doorToBeOpenForTeleport.current_state == "Door closed"){
                        bothDoorsUnlocked = false;
                    }
                }
                if (monsterPosition2.doorToBeOpenForTeleport!= null){
                    if (monsterPosition2.doorToBeOpenForTeleport.current_state == "Door closed"){
                        bothDoorsUnlocked = false;
                    }
                } 
                if (bothDoorsUnlocked){
                    Vector3 startPosition = child.GetChild(0).transform.position;
                    gameObject.GetComponent<NavMeshAgent>().Warp(startPosition);
                    waypoint = child.GetChild(1).gameObject;
                    Debug.Log("Found and up and running");
                    state=0;
                    break;
                }
            }
            Debug.Log(waypoint.transform.parent.name);
            
        }
        StartCoroutine(SetFollowingPlayerWithDelay());
        
    }

    IEnumerator SetFollowingPlayerWithDelay(){//otherwise there is a small annoying bug
        yield return new WaitForSeconds(0.5f);
        followingPlayer = true;
    }
    IEnumerator FadeMonster(){
        FadeChildren(transform);
        yield return new WaitForSeconds(fadeAway);
        int teleportPositionIndex = Random.Range(0,monsterTeleportPositions.transform.childCount-1);
        gameObject.GetComponent<NavMeshAgent>().isStopped = false;
        
        StartFollowing();
    }
    IEnumerator FadeRenderer(SkinnedMeshRenderer renderer){
        
        Color newColor = renderer.materials.First<Material>().color;
        float lastTime = Time.time;
        while(newColor.a>0)
        {
            float currentTime = Time.time;
            float dt = currentTime-lastTime;
            lastTime = currentTime;
            newColor.a = newColor.a - dt/fadeAway;
            renderer.materials.First<Material>().color = newColor;
            yield return new WaitForSeconds(.01f);
        }
        
        newColor = renderer.material.color;
        newColor.a = 1;
        renderer.material.color = newColor;

        yield return null;
        
    }
}
