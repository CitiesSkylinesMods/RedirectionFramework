using System;

namespace TreeUnlimiter.RedirectionFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RedirectMethodAttribute : RedirectAttribute
    {
        public RedirectMethodAttribute() : base(false)
        {
        }

        public RedirectMethodAttribute(bool onCreated) : base(onCreated)
        {
        }
    }
}
