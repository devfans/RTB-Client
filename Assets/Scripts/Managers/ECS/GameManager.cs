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

namespace RTBClient
{
    public class RTBTankWorld: RTBECSWorld
    {
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_watchDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control.


        private WaitForSeconds m_watchWait;         // Used to have a delay whilst the round starts.

        public Tank[] m_tanks;
        private bool m_watching = false;

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
            foreach (var entity in Entities)
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
            StartCoroutine(GameLoop());
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
            SetupNetwork();
            m_CameraControl = ApplicationManager.FindObjectOfType<CameraControl>();
            Transform[] targets = new Transform[2];

            m_MessageText = ApplicationManager.FindObjectOfType<Text>();
            m_MessageText.text = "";
            m_TankPrefab = (GameObject)Resources.Load("TankECS", typeof(GameObject));

            var tank1 = new Tank() { name = "player 1" };
            var tank2 = new Tank() { name = "player 2" };
            tank1.Setup();
            tank2.Setup();

            var movement1 = (MovementComponent)ComponentManager.GetEntityComponent(tank1.m_code, MovementComponent.s_code);
            movement1.position = new Vector3(15, 0, 2);
            movement1.velocity = 5;
            movement1.axisMoveName = "Vertical1";

            movement1.turnSpeed = 180;
            movement1.axisTurnName = "Horizontal1";

            var movement2 = (MovementComponent)ComponentManager.GetEntityComponent(tank2.m_code, MovementComponent.s_code);
            movement2.position = new Vector3(-10, 0, 2);
            movement2.velocity = 5;
            movement2.axisMoveName = "Vertical2";

            movement2.turnSpeed = 180;
            movement2.axisTurnName = "Horizontal2";


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
            var preprocess = new RTBPreProcessSystem(this);
            AttachSystem(preprocess);

            var movement = new MovementSystem(this);
            movement.m_enabled = false;
            AttachSystem(movement);

            var postprocess = new RTBPostProcessSystem(this);
            AttachSystem(postprocess);
        }
    }
}