﻿namespace Compose
{
	public abstract class DirectDefaultTransition<TService, TDefault> 
		: DefaultDirectTransition<TService, TDefault> where TDefault : TService
	{
		public DirectDefaultTransition(TService service) : base(service) { }

		public override bool Change<TImplementation>(TImplementation service)
		{
			Service = service;
			return true;
		}
	}
}