using System;

namespace TreeUnlimiter.RedirectionFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RedirectReverseAttribute : RedirectAttribute
    {
        public RedirectReverseAttribute() : base(false)
        {
        }

        public RedirectReverseAttribute(bool onCreated) : base(onCreated)
        {
        }
    }
}