using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ECS;
using MessageQueue = System.Collections.Concurrent.ConcurrentQueue<Message>;
using PlayerId = System.UInt32;

namespace RTBClient
{
    public class RTBClientSession: ISession
    {
        private Logging.Logger logger = Logging.Logger.GetLogger("RTBClientSession");
        private MessageQueue m_queue = new MessageQueue();

        public void Handle(string str)
        {
            logger.info(string.Format("REV: {0}", str));
            SendString(str);
        }

        public void HandleMessage(Message message)
        {
            logger.debug("Message rev: {0}, current queue length {1}", message.type, m_queue.Count);
            m_queue.Enqueue(message);
        }

        public void Setup()
        {
            AttachSessionStatusDelegates(OnConnected, SessionStatus.Connected);
            AttachSessionStatusDelegates(OnConnecting, SessionStatus.Connecting);
            // AttachStringRevHandler(Handle);
            AttachMessageRevHandler(HandleMessage);
        }

        public void OnConnected()
        {
            logger.info("Connection established!");
        }

        public void OnConnecting()
        {
            logger.info("Connecting to server!");
        }

        public Message GetMessage()
        {
            Message message;
            m_queue.TryDequeue(out message);
            return message;
        }

        public int GetMessageLength()
        {
            return m_queue.Count;
        }

        public void SendMessage(Message message)
        {
            SendData(MessageManager.Encode(message));
        }

    }

    public class TankWorldECS : IECSWorld
    {
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_watchDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control.

        
        private WaitForSeconds m_watchWait;         // Used to have a delay whilst the round starts.


        public Tank[] m_tanks;

        private bool m_watching = false;

        public string m_ip = "127.0.0.1";
        public int m_port = 5050;
        public RTBClientSession m_session;
        public PlayerId m_player = 0;


        public override void OnReady()
        {
            Start();
        }

        public void SetupNetwork()
        {
            m_session = NetworkManager.CreateSession<TcpProtocol, RTBClientSession>(m_ip, m_port);
            m_session.Setup();

            m_session.Connect();
        }

        public override Message GetNetMessage()
        {
            return m_session.GetMessage();
        }

        public override int GetNetMessageLength()
        {
            return m_session.GetMessageLength();
        }

        public override void SendNetMessage(Message message)
        {
            m_session.SendMessage(message);
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
            var movement = new MovementSystem(this);
            movement.m_enabled = false;
            AttachSystem(movement);
        }


        private void Start()
        {
            logger.info("Starting tank game ecs");
            // Create the delays so they only have to be made once.
            m_watchWait = new WaitForSeconds (m_watchDelay);

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

        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(GameInit());
            yield return StartCoroutine(GameMeta());
            yield return StartCoroutine(GameStart());
        }

        private IEnumerator GameInit()
        {
            while (true)
            {
                Message message = GetNetMessage();
                if (message != null && message.type == MessageType.BattleInit)
                {
                    logger.info("Battle init success!");
                    break;
                }
                else
                {
                    SendNetMessage(new BattleInitMessage() { player = m_player });
                    yield return new WaitForSeconds(1);
                }
            }
            yield return null;
        }

        private IEnumerator GameMeta()
        {
            while (true)
            {
                Message message = GetNetMessage();
                if (message != null && message.type == MessageType.BattleMeta)
                {
                    var msg = (BattleMetaMessage)message;
                    //TODO  Setup battle meta
                    m_player = msg.player;
                    SendNetMessage(new BattleMetaMessage() { player = m_player });
                    logger.debug("Battle meta retrieve success, player id {0}!", m_player);
                    break;
                }
                else
                {
                    yield return new WaitForSeconds(1);
                }
            }
            yield return null;
        }

        private IEnumerator GameStart()
        {
            while (true)
            {
                Message message = GetNetMessage();
                if (message != null && message.type == MessageType.BattleStart)
                {
                    SendNetMessage(new BattleStartMessage() { });
                    logger.info("Battle is starting");
                    break;
                }
                else
                {
                    yield return new WaitForSeconds(1);
                }
            }
            yield return null;
        }

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
            foreach(var entity in Entities)
            {
                logger.info(string.Format("Checking entity {0} component{1}", entity.m_code.str(), MovementComponent.s_code.str()));
                MovementComponent movement = (MovementComponent)ComponentManager.GetEntityComponent(entity.m_code, MovementComponent.s_code);
                logger.info(string.Format("posistion: {0}", movement.position));
            }
        }

    }
}