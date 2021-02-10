using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Neo.IronLua;

namespace LuaSettings
{
	public class LuaDebuggerException : Exception
	{
		public string LastFrameSourceName { get; }
		public string LastTraceSourceName { get; }
		
		public string LastFrameScope { get; }
		public int LastFrameLine { get; }
		
		public int LastTraceLine { get; }
		public string LastTraceScope { get; }

		public LuaDebuggerException(string message, string lastFrameSourceName, string lastFrameScope, int lastFrameLine,
			string lastTraceSourceName, string lastTraceScope, int lastTraceLine) : base(message)
		{
			LastFrameSourceName = lastFrameSourceName;
			LastTraceSourceName = lastTraceSourceName;
			LastFrameScope = lastFrameScope;
			LastFrameLine = lastFrameLine;
			LastTraceLine = lastTraceLine;
			LastTraceScope = lastTraceScope;
		}
	}

	public class LuaDebugger : LuaTraceLineDebugger
	{
		private string _lastFrameSourceName = "";
		private string _lastTraceSourceName = "";

		private Stack<string> _lastFrameScope = new Stack<string>();
		private int _lastFrameLine = 0;

		private int _lastTraceLine = 0;
		private string _lastTraceScope = "";

		protected override void OnExceptionUnwind(LuaTraceLineExceptionEventArgs e)
		{
			base.OnExceptionUnwind(e);
			var message = $"Exception caught: {e.Exception} On line: {e.SourceLine} in source {e.SourceName} and scope {e.ScopeName}. Last frame scope {_lastFrameScope.Peek()} trace line before exception unwind: {_lastTraceLine}.";

			throw new LuaDebuggerException(message,
				_lastFrameSourceName,
				_lastFrameScope.Peek(),
				_lastFrameLine,
				_lastTraceSourceName,
				_lastTraceScope,
				_lastTraceLine);
		}

		protected override void OnTracePoint(LuaTraceLineEventArgs e)
		{
			base.OnTracePoint(e);
			_lastTraceSourceName = e.SourceName;
			_lastTraceScope = e.ScopeName;
			_lastTraceLine = e.SourceLine;
		}

		protected override void OnFrameEnter(LuaTraceLineEventArgs e)
		{
			base.OnFrameEnter(e);

			_lastFrameSourceName = e.SourceName;
			_lastFrameLine = e.SourceLine;
			_lastFrameScope.Push(e.ScopeName);
		}

		protected override void OnFrameExit()
		{
			base.OnFrameExit();
			_lastFrameScope.Pop();
		}

		protected override LuaTraceChunk CreateChunk(Lua lua, LambdaExpression expr)
		{
			return base.CreateChunk(lua, expr);
		}
	}
}
