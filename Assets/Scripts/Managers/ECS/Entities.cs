using System;
using ECS;
namespace RTBClient
{ 
    public class Tank: Entity
    {
        public string name;

        public new void Setup()
        {
            var movement = new MovementComponent() { velocity = 2 };
            var actor = new ActorComponent();

            var components = new Component[] { movement, actor };
            ComponentManager.SetupEntityComponents(this, components);

            m_components = MovementComponent.s_code | ActorComponent.s_code;
        }
    }
}