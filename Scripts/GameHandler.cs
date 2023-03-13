using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;


public class GameHandler : MonoBehaviour
{

    public bool loadingFromIndividual = false;
    private AudioSource gameAudioSource;

    [Header("Settup")]
    public ObjectPooler objPooler;

    [Header("GameSettings")]
    public bool debugMode = false;
    public bool findEnemiesDynamically;
    public Transform spawnPosition;
    public Transform defaultCameraPosition;

    public Camera gameCamera;
    private Camera playerCamera;

    [Header("Pause settings")]
    public GameObject pauseMenuObject;
    public TextMeshProUGUI pauseLevelText;

    [Header("Map settings")]
    public Button returnToMenuButton;
    public GameObject mapGenerationUi;

    public NavMeshSurface nMeshSurface;
    public float mapGenerationTime = 3f;
    [Space]
    public GameObject emptyMapTile;
    public Transform mapHolder;
    public Transform enemyHolder;
    [Space]
    public int baseXSize = 10;
    public int baseZSize = 10;
    public float tileSize = 5f;
    [Space]
    public float obstacleHeightOffset = 0.5f;
    [Space]
    public float obstacleChance = 35f;
    public float utilityChance = 15f;
    [Space]
    public int minEnemies = 1;
    public int maxEnemies = 15;

    public Obstacle[] spawnableObstacles;
    List<Obstacle> defaultObstacles;
    List<Obstacle> utilityObstacles;

    [Header("Level settings")]
    public AudioClip[] victorySounds;
    public GameObject clearLevelHolder;
    [Space]
    public SpawnableEnemy[] spawnableEnemies;
    [Space]
    public GameObject gameOverHolder;
    public TextMeshProUGUI reachedLevelText;
    public TextMeshProUGUI levelNotifierText;

    [Header("Notif settings")]
    public TextMeshProUGUI notifText;
    public GameObject notifObject;
    [Header("randvals")]
    public float holePercent = 15f;

    [Header("Audio")]
    public AudioClip[] killSounds;


    // Start is called before the first frame update
    void Start()
    {
        //set up pause
        paused = false;
        pauseMenuObject.SetActive(false);

        //set up menu
        returnToMenuButton.onClick.AddListener(LoadMenu);

        mapGenerationUi.SetActive(false);

        //deactivate ui
        gameOverHolder.SetActive(false);
        clearLevelHolder.SetActive(false);

        gameAudioSource = GetComponent<AudioSource>();

        //set up obstacles
        utilityObstacles = new List<Obstacle>();
        defaultObstacles = new List<Obstacle>();

        foreach (Obstacle ob in spawnableObstacles)
        {
            switch (ob.obstacleType)
            {
                case Obstacle.ObstacleType.obstacle:
                    defaultObstacles.Add(ob);
                    break;
                case Obstacle.ObstacleType.utility:
                    utilityObstacles.Add(ob);
                    break;
            }
        }

        if (loadingFromIndividual)
        {
            LoadMenu();
        }
        else
        {
        StartGame();
        }
    }

    
    GameObject player;
    void GetPlayer()
    {
        player = GameObject.Find("Player");
    }
    void GetPlayerCamera()
    {
        playerCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    int curStage = 0;

    private List<GameObject> aliveEnemies;
    void SpawnAllEnemies()
    {
        if (findEnemiesDynamically)
        {
            aliveEnemies = GameObject.FindGameObjectsWithTag("Enemy").ToList<GameObject>();
        }
        foreach (GameObject enemy in aliveEnemies)
        {
            SpawnEnemyInfo(enemy);
        }  
    }

    void SpawnEnemyInfo(GameObject enemy)
    {
        EnemyBrain enemyBrain = enemy.GetComponent<EnemyBrain>();
        enemyBrain.SetTarget(player);

        enemyBrain.Spawn(this,mapGenerationTime + 2f);;
    }

    private float lastNotif;
    public void EnemyDied(EnemyBrain eBrain)
    {
        notifText.text = "KilledEnemy";
        notifObject.SetActive(true);
        lastNotif = Time.time + 1f;

        //make sound
        AudioClip targetSound = killSounds[Random.Range(0, killSounds.Length)];
        PlaySound(targetSound);

        if (aliveEnemies.Contains(eBrain.gameObject))
        {
            aliveEnemies.Remove(eBrain.gameObject);
        }
        if(aliveEnemies.Count == 0)
        {
            Completelevel();
        }
    }
    #region level Managing
    void StartGame()
    {
        //get already spawned enemies
        curStage = 0;
        GetPlayer();
        GetPlayerCamera();
        if (debugMode)
        {
            SetCameraMode(player);
            SpawnPlayer(true);
            SpawnAllEnemies();
        }
        else
        {
            GenerateMap();
        }
    }

    private List<GameObject> generatedTiles;
    private List<GameObject> generatedObstacles;

    void DestroyOldMap()
    {
        if (generatedTiles != null)
        {
            if (generatedTiles.Count > 0)
            {
                foreach (GameObject g in generatedTiles)
                {
                    Destroy(g);
                }
            }
        }

        if (generatedObstacles != null)
        {
            if (generatedObstacles.Count > 0)
            {
                foreach (GameObject g in generatedObstacles)
                {
                    Destroy(g);
                }
            }
        }
    }

    private void FixedUpdate()
    {
    //this is beacuse we must remove old map on different frame to generating navmesh
        if(wantsToGenerate && destroyingMap == false)
        {
            wantsToGenerate = false;
            StartCoroutine(MapGeneration());
        }

        if (destroyingMap)
        {
            DestroyOldMap();
            destroyingMap = false;
        }
    }
    bool wantsToGenerate = false;
    bool destroyingMap;
    //generate map
    void GenerateMap()
    {
        SetCameraMode(false);

        destroyingMap = true;
        wantsToGenerate = true;
    }

    private bool generatingMap = false;
    IEnumerator MapGeneration()
    {
        mapGenerationUi.SetActive(true);

        generatingMap = true;

        generatedTiles = new List<GameObject>();
        generatedObstacles = new List<GameObject>();

        //generate new tiles
        for (int x = 0; x < baseXSize; x++)
        {
            for (int z = 0; z < baseZSize; z++)
            {

                //check to see if make tile
                float chance = Random.Range(0f, 100f);
                Vector3 spawnVector = new Vector3((x * tileSize) - (baseXSize * 0.5f * tileSize), 0f, (z * tileSize) - (baseZSize * 0.5f * tileSize));
                if (chance < holePercent && Vector3.Distance(Vector3.zero, spawnVector) > tileSize)
                {
                    //we have a hole
                }
                else
                {
                    //we create a tile
                    GameObject instance = Instantiate(emptyMapTile, spawnVector, Quaternion.identity, mapHolder);
                    generatedTiles.Add(instance);
                }
            }
        }

        //generate obstacles
        foreach (GameObject tile in generatedTiles)
        {
            if (Vector3.Distance(tile.transform.position, Vector3.zero) > tileSize)
            {
                Vector3 obstaclePosition = tile.transform.position;
                obstaclePosition.y += obstacleHeightOffset;
                //check to see if make tile
                float chance = Random.Range(0f, 100f);
                if (chance < utilityChance)
                {
                    Obstacle targetObstace = utilityObstacles[Random.Range(0, utilityObstacles.Count)];
                    int multiplier = Random.Range(0, 5);
                    float angle = multiplier * 90f;
                    GameObject instance = Instantiate(targetObstace.obstaclePrefab, obstaclePosition, Quaternion.AngleAxis(angle, Vector3.up), mapHolder);
                    generatedObstacles.Add(instance);
                }
                else
                {
                    if (chance < obstacleChance)
                    {
                        Obstacle targetObstace = defaultObstacles[Random.Range(0, defaultObstacles.Count)];
                        GameObject instance = Instantiate(targetObstace.obstaclePrefab, obstaclePosition, Quaternion.AngleAxis(Random.Range(0f, 180f), Vector3.up), mapHolder);
                        generatedObstacles.Add(instance);
                    }
                }
            }
        }

        //spawn enemies
        aliveEnemies = new List<GameObject>();

        GameObject[] allSpawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawn");
        List<GameObject> useSpawns = Fisher_Yates_CardDeck_Shuffle(allSpawnPoints.ToList<GameObject>());

        int enemiesToSpawn = Mathf.Clamp(curStage + Random.Range(-1, 4), minEnemies, maxEnemies);

        List<int> usedSpawnIndexes = new List<int>();

        //we spawn
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            //we choose
            float difficultyInput = Mathf.Clamp01(curStage / 20f);
            int difficultyValue = (int)Mathf.Clamp(5f * difficultyInput, 0f, 5f);

            List<GameObject> possibleEnemiesToSpawn = new List<GameObject>();
            foreach (SpawnableEnemy spawnInput in spawnableEnemies)
            {
                if(spawnInput.enemyDifficulty <= difficultyValue)
                {
                    possibleEnemiesToSpawn.Add(spawnInput.enemyPrefab);
                }
            }

            GameObject toSpawn = possibleEnemiesToSpawn[Random.Range(0, possibleEnemiesToSpawn.Count)];
            //this is to stop doubling up on positions
            int useIndex = Random.Range(0, useSpawns.Count);
            if (usedSpawnIndexes.Contains(useIndex))
            {
                for (int z = 0; z < useSpawns.Count; z++)
                {
                    if (!usedSpawnIndexes.Contains(z))
                    {
                        useIndex = z;
                        break;
                    }
                }
            }
            usedSpawnIndexes.Add(useIndex);
            
            Vector3 spawnPos = useSpawns[useIndex].transform.position;
            GameObject enemyInstance = Instantiate(toSpawn, spawnPos, Quaternion.identity, enemyHolder);
            aliveEnemies.Add(enemyInstance);

            GameObject particleInstance =  objPooler.SpawnFromPool("EnemySpawnParticles", spawnPos, Quaternion.identity);
            particleInstance.GetComponent<ParticleSystem>().Play();
        }

        SpawnAllEnemies();
        //make navmesh

        nMeshSurface.RemoveData();
        nMeshSurface.BuildNavMesh();
        

        yield return new WaitForSeconds(mapGenerationTime);

        SetCameraMode(player);

        if(curStage == 0)
        {
            SpawnPlayer(true);
        }
        else
        {
            SpawnPlayer(false);
        }
        curStage++;

        levelNotifierText.text = "Current Stage: " + curStage.ToString();

        generatingMap = false;

        clearLevelHolder.SetActive(false);
        mapGenerationUi.SetActive(false);
    }
    void Completelevel()
    {
        Debug.Log("Completed level");

        StartCoroutine(CompletedLevelCoroutine());
    }

    IEnumerator CompletedLevelCoroutine()
    {
        GetPlayer();
        player.GetComponent<ConnectionManager>().LevelEnded();

        PlayerHealthManager p_Health = player.GetComponent<PlayerHealthManager>();
        p_Health.GetPlayerUi().SetActive(false);
        p_Health.BlockDamageAllowed();

        clearLevelHolder.SetActive(true);

        AudioClip targClip = victorySounds[Random.Range(0, victorySounds.Length)];
        PlaySound(targClip);

        yield return new WaitForSeconds(6.5f);
        GetPlayerCamera();
        if (!debugMode)
        {
            GenerateMap();
        }
    }

    public void EndGame()
    {
        SetCursorLock(false);
        gameOverHolder.SetActive(true);
        reachedLevelText.text = "Stage Score: " + curStage.ToString();

        SaveScore();
    }

    void SaveScore()
    {
        int high = PlayerPrefs.GetInt("HighScore", 0);
        if(high < curStage)
        {
            PlayerPrefs.SetInt("HighScore", curStage);
        }
    }

    private bool playerDead = false;
    public void PlayerDie()
    {
        playerDead = true;
        player.GetComponent<PlayerHealthManager>().GetPlayerUi().SetActive(false);

        foreach (GameObject en in aliveEnemies)
        {
            en.GetComponent<EnemyBrain>().SetPlayerDead();
        }

    }
    #endregion

    void SpawnPlayer(bool defaultValues)
    {
        playerDead = false;

        player.transform.position = spawnPosition.position;
        PlayerHealthManager p_healthManager = player.GetComponent<PlayerHealthManager>();
        p_healthManager.SpawnPlayer();

        player.GetComponent<ConnectionManager>().ResetAnimator();

        if (defaultValues == true)
        {
            p_healthManager.SetDefaultValue();
            player.GetComponent<ConnectionManager>().SetDefaultValues();
            player.GetComponent<PlayerMovement>().SetDefaultValues();
            player.GetComponent<KickManager>().SetDefaultValues();
        }
    }
    
    
    void SetCameraMode(bool isPlayerCamera)
    {
        if (isPlayerCamera)
        {
            SetCursorLock(true);
            playerCamera.enabled = true;
            player.GetComponent<PlayerHealthManager>().GetPlayerUi().SetActive(true);
            
            gameCamera.enabled = false;
        }
        else
        {
            playerCamera.enabled = false;
            player.GetComponent<PlayerHealthManager>().GetPlayerUi().SetActive(false);
  
            gameCamera.enabled = true;

            SetCursorLock(false);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if(Time.time > lastNotif)
        {
            if (notifObject.activeSelf)
            {
                notifObject.SetActive(false);
            }
        }
    }

    [HideInInspector] public bool cursorLocked = false;
    public void SetCursorLock(bool value)
    {
        if (value == true)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        cursorLocked = value;
    }

    void PlaySound(AudioClip a_Aclip)
    {
        gameAudioSource.PlayOneShot(a_Aclip);
    }

    public static List<GameObject> Fisher_Yates_CardDeck_Shuffle(List<GameObject> aList)
    {

        System.Random _random = new System.Random();

        GameObject myGO;

        int n = aList.Count;
        for (int i = 0; i < n; i++)
        {
            // NextDouble returns a random number between 0 and 1.
            // ... It is equivalent to Math.random() in Java.
            int r = i + (int)(_random.NextDouble() * (n - i));
            myGO = aList[r];
            aList[r] = aList[i];
            aList[i] = myGO;
        }

        return aList;
    }


    #region scene loading

    public void LoadMenu()
    {
        SaveScore();

        CancelPause();
        SetCursorLock(false);
        SceneManager.LoadScene("MainMenu");
    }

    #endregion


    #region pausing
    private bool paused = false;
    public bool IsPaused()
    {
        return paused;
    }

    public void PauseInput(InputAction.CallbackContext context)
    {
        if (context.performed && !playerDead)
        {
            if (paused)
            {
                CancelPause();
            }
            else
            {
                StartPause();
            }
        }
    }

    private bool targetCursorLock;
    private float targetTimeScale = 1f;
    void CancelPause()
    {
        player.GetComponent<PlayerHealthManager>().GetPlayerUi().SetActive(true);

        paused = false;
        pauseMenuObject.SetActive(false);

        SetCursorLock(targetCursorLock);

        Time.timeScale = targetTimeScale;
    }

    void StartPause()
    {
        //so we can return to original timing
        player.GetComponent<PlayerHealthManager>().GetPlayerUi().SetActive(false);

        targetTimeScale = Time.timeScale;
        targetCursorLock = cursorLocked;

        SetCursorLock(false);

        paused = true;
        pauseMenuObject.SetActive(true);

        pauseLevelText.text = "Current stage: " + curStage.ToString();

        Time.timeScale = 0f;
    }


    #endregion
}

[System.Serializable]
public struct SpawnableEnemy
{
    public int enemyDifficulty; //out of 5
    public GameObject enemyPrefab;
}
