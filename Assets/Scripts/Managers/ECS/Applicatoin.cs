using System;
using UnityEngine;

public class TankGameECS : IApplicationInstance
{
    private Logging.Logger logger = Logging.Logger.GetLogger("TankGameECS");

    public override void OnCreate()
    {
        logger.info("Creating tank game ecs world");
        RTBClient.TankWorldECS world = (RTBClient.TankWorldECS)WorldManager.NewWorld<RTBClient.TankWorldECS>();

    }

    public override void OnEnter()
    {

    }

    public override void OnExit()
    {
    }
}
