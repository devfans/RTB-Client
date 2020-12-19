using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ECS;
using MessageQueue = System.Collections.Concurrent.ConcurrentQueue<Message>;
using MessagePipe = System.Collections.Concurrent.BlockingCollection<GameAction>;
using PlayerId = System.UInt32;
using ActionList = System.Collections.Generic.List<GameAction>;
using LevelStore;

namespace RTBClient
{
    public class RTBTankWorld: RTBECSWorld
    {
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_watchDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control.
        public InputField input_serverIp;
        public InputField input_serverPort;
        public Button button_connect;
        public Text text_status;


        private WaitForSeconds m_watchWait;         // Used to have a delay whilst the round starts.

        public Tank[] m_tanks;
        private bool m_watching = false;

        private ECS.System m_preprocessSystem; 
        private ECS.System m_postprocessSystem; 
        private ECS.System m_movementSystem;

        private StoreClient client = new StoreClient("http://localhost:8001/v1");


        private IEnumerator WatchGameLoop()
        {
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
            WatchGame();
            yield return m_watchWait;

            if (m_watching)
                StartCoroutine(WatchGameLoop());
        }

        private void WatchGame()
        {
            foreach (var entity in Entities.Values)
            {
                logger.info(string.Format("Checking entity {0} component{1}", entity.m_code.str(), MovementComponent.s_code.str()));
                MovementComponent movement = (MovementComponent)ComponentManager.GetEntityComponent(entity.m_code, MovementComponent.s_code);
                logger.info(string.Format("posistion: {0}", movement.position));
            }
        }

        public override void Start()
        {
            logger.info("Starting tank game ecs");
            // Create the delays so they only have to be made once.
            m_watchWait = new WaitForSeconds(m_watchDelay);

            // SpawnAllTanks();
            button_connect.onClick.AddListener(Connect);
			// test level state
			StartCoroutine(client.ListLevels(ListLevels));
            StartCoroutine(client.SaveLevel("dexter", "ddddd", SavedLevel));
            
            
		}

        private void GotLevelRevisions(in StoreResponse<List<string>> res) {
            Debug.Log(JsonUtility.ToJson(res));

            StartCoroutine(client.FetchLevel("dexter", res.result[0], GotLevelRevision));
        }

        private void GotLevelRevision(in StoreResponse<LevelState> res)
        {
            Debug.Log(JsonUtility.ToJson(res));

        }

        private void SavedLevel(in StoreResponse<object> res) {
            Debug.Log(JsonUtility.ToJson(res));
            StartCoroutine(client.ListLevelRevisions("dexter", GotLevelRevisions));
        }

        public void Connect()
        {
            m_ip = input_serverIp.text;
            m_port = Int32.Parse(input_serverPort.text);
            text_status.text = string.Format("Connecting to {0}:{1}", m_ip, m_port);
            logger.debug("Connecting to {0}:{1}", m_ip, m_port);

            SetupNetwork();
            StartCoroutine(GameLoop());
        }

	    private void ListLevels(in StoreResponse<List<string>> res)
	    {
            foreach (string path in res.result) {
				Debug.Log(path);
			}
	    }


	    private void SpawnAllTanks()
        {
            // For all the tanks...
            for (int i = 0; i < m_tanks.Length; i++)
            {

            }
        }

        public override void Setup()
        {
            // SetupNetwork();
            m_CameraControl = ApplicationManager.FindObjectOfType<CameraControl>();
            Transform[] targets = new Transform[2];

            m_MessageText = ApplicationManager.FindObjectOfType<Text>();
            m_MessageText.text = "";
            m_TankPrefab = (GameObject)Resources.Load("TankECS", typeof(GameObject));

            input_serverIp = GameObject.Find("ServerIp").GetComponent<InputField>();
            input_serverPort = GameObject.Find("ServerPort").GetComponent<InputField>();
            button_connect = GameObject.Find("Connect").GetComponent<Button>();
            text_status = GameObject.Find("Status").GetComponent<Text>();

            var tank1 = new Tank() { name = "player 1" };
            var tank2 = new Tank() { name = "player 2" };
            tank1.Setup();
            tank2.Setup();

            var movement1 = (MovementComponent)ComponentManager.GetEntityComponent(tank1.m_code, MovementComponent.s_code);
            movement1.position = new Vector3(15, 0, 2);
            movement1.velocity = 5;
            movement1.axisMoveName = "Vertical1";
            movement1.inputEnabled = true;

            movement1.turnSpeed = 180;
            movement1.axisTurnName = "Horizontal1";

            var movement2 = (MovementComponent)ComponentManager.GetEntityComponent(tank2.m_code, MovementComponent.s_code);
            movement2.position = new Vector3(-10, 0, 2);
            movement2.velocity = 5;
            movement2.axisMoveName = "Vertical1";
            movement2.inputEnabled = true;

            movement2.turnSpeed = 180;
            movement2.axisTurnName = "Horizontal1";


            ActorComponent actor1 = (ActorComponent)ComponentManager.GetEntityComponent(tank1.m_code, ActorComponent.s_code);
            actor1.instance = EntityManager.SpawnInstance(m_TankPrefab, movement1.position, movement1.rotation);

            ActorComponent actor2 = (ActorComponent)ComponentManager.GetEntityComponent(tank2.m_code, ActorComponent.s_code);
            actor2.instance = EntityManager.SpawnInstance(m_TankPrefab, movement2.position, movement2.rotation);

            AttachEntity(tank1);
            AttachEntity(tank2);
            m_tanks = new Tank[] { tank1, tank2 };

            targets[0] = actor1.instance.transform;
            targets[1] = actor2.instance.transform;

            m_CameraControl.m_Targets = targets;

            SetupSystem();
        }

        public void SetupSystem()
        {
            m_preprocessSystem = new RTBPreProcessSystem(this);
            m_preprocessSystem.m_enabled = false;
            AttachSystem(m_preprocessSystem);

            m_movementSystem = new RTBMovementSystem(this);
            m_movementSystem.m_enabled = false;
            AttachSystem(m_movementSystem);

            m_postprocessSystem = new RTBPostProcessSystem(this);
            m_postprocessSystem.m_enabled = false;
            AttachSystem(m_postprocessSystem);
        }

        public override void SetupGameMeta()
        {
            base.SetupGameMeta();
           
            m_tanks[0].m_local = m_player == 2;
            m_tanks[1].m_local = m_player == 1;

            foreach( var tank in m_tanks)
            {
                var movement = (MovementComponent)ComponentManager.GetEntityComponent(tank.m_code, MovementComponent.s_code);
                logger.debug("player {0}, position {1}, rotation {2}, velocity {3}, turnSpeed {4}, axisMove {5}, axisTurn {6}",
                    tank.m_local? "local" : "remote",
                    movement.position, movement.rotation, movement.velocity, 
                    movement.turnSpeed,
                    movement.axisMoveName, movement.axisTurnName);
            }

        }

        public override void StartBattle()
        {
            base.StartBattle();
            text_status.text = string.Format("Connected to {0}:{1}", m_ip, m_port);
            logger.info(string.Format("Connected to {0}:{1}", m_ip, m_port));
            button_connect.gameObject.SetActive(false);
            input_serverIp.gameObject.SetActive(false);
            input_serverPort.gameObject.SetActive(false);
            m_MessageText.text = "";
            m_preprocessSystem.m_enabled = true;
            m_movementSystem.m_enabled = true;
            m_postprocessSystem.m_enabled = true;
        }


    }
}