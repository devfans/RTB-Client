using System;
using UnityEngine;

public class TankGame: IApplicationInstance
{
    public Logging.Logger logger = Logging.Logger.GetLogger("TankGame");

    public override void OnCreate()
    {
        logger.info("Creating tank game world");
        RTBClient.TankWorld world = (RTBClient.TankWorld)WorldManager.NewWorld<RTBClient.TankWorld>();

    }

    public override void OnEnter()
    {

    }

    public override void OnExit()
    {
    }
}
