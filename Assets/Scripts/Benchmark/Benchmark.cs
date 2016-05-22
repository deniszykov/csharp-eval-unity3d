/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "C# Eval()" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706
	
	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND 
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE 
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY, 
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE 
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions;
using GameDevWare.Dynamic.Expressions.CSharp;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class Benchmark : MonoBehaviour
{
	const int Iterations = 100000;

	internal void Awake()
	{
		Debug.Log(SystemInfo.processorType);
	}
	internal void OnGUI()
	{
		var width = 500.0f;
		var height = Screen.height - 40.0f;

		GUILayout.BeginArea(new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height));
		GUILayout.BeginVertical();

		if (GUILayout.Button("Measure Expression Performance"))
			new Action(this.MeasureExpressionPerformance).BeginInvoke(null, null);

		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	private void MeasureExpressionPerformance()
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		try
		{
			const string expression = "(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\")";

			CSharpExpression.Evaluate<double>(expression);

			// tokenization
			var tokens = Tokenizer.Tokenize(expression);

			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				Tokenizer.Tokenize(expression);
			sw.Stop();
			var tokenization = sw.Elapsed;

			// parsing
			var parseTree = Parser.Parse(tokens);
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				Parser.Parse(tokens);
			sw.Stop();
			var parsing = sw.Elapsed;

			// binding
			var expressionBuilder = new ExpressionBuilder(new ParameterExpression[0], typeof(double));
			var expressionTree = expressionBuilder.Build(parseTree.ToExpressionTree(checkedScope: true));

			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				expressionBuilder.Build(parseTree.ToExpressionTree(checkedScope: true));
			sw.Stop();
			var binding = sw.Elapsed;


			// compilation JIT
			var expressionLambda = Expression.Lambda<Func<double>>(expressionTree, expressionBuilder.Parameters);
			var fnJit = expressionLambda.Compile();
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
			{
				expressionLambda = Expression.Lambda<Func<double>>(expressionTree, expressionBuilder.Parameters);
				expressionLambda.Compile();
			}
			sw.Stop();
			var compilationJit = sw.Elapsed;

			// compilation AOT
			expressionLambda = Expression.Lambda<Func<double>>(expressionTree, expressionBuilder.Parameters);
			var fnAot = expressionLambda.CompileAot(forceAot: true);
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
			{
				expressionLambda = Expression.Lambda<Func<double>>(expressionTree, expressionBuilder.Parameters);
				expressionLambda.CompileAot(forceAot: true);
			}
			sw.Stop();
			var compilationAot = sw.Elapsed;

			// evaluation JIT
			fnAot();
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				fnAot();
			sw.Stop();
			var evalAot = sw.Elapsed;
			var totalAot = (tokenization + parsing + binding + compilationAot + evalAot);


			// evaluation AOT
			fnJit();
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				fnJit();
			sw.Stop();
			var evalJit = sw.Elapsed;
			var totalJit = (tokenization + parsing + binding + compilationJit + evalJit);

			Debug.Log(string.Format("Tokenization: {0:F2} | {1:F5} | {2:F1}%", tokenization.TotalMilliseconds, tokenization.TotalMilliseconds / Iterations, tokenization.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
			Debug.Log(string.Format("Parsing: {0:F2} | {1:F5} {2:F1}%", parsing.TotalMilliseconds, parsing.TotalMilliseconds / Iterations, parsing.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
			Debug.Log(string.Format("Binding: {0:F2} | {1:F5} {2:F1}%", binding.TotalMilliseconds, binding.TotalMilliseconds / Iterations, binding.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
			Debug.Log(string.Format("Compilation (JIT): {0:F2} | {1:F5} {2:F1}%", compilationJit.TotalMilliseconds, compilationJit.TotalMilliseconds / Iterations, compilationJit.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
			Debug.Log(string.Format("Compilation (AOT): {0:F2} | {1:F5} {2:F1}%", compilationAot.TotalMilliseconds, compilationAot.TotalMilliseconds / Iterations, compilationAot.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
			Debug.Log(string.Format("Evaluation (AOT): {0:F2} | {1:F5} {2:F1}%", evalAot.TotalMilliseconds, evalAot.TotalMilliseconds / Iterations, evalAot.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
			Debug.Log(string.Format("Total (AOT): {0:F2} | {1:F5} {2:F1}%", totalAot.TotalMilliseconds, totalAot.TotalMilliseconds / Iterations, totalAot.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
			Debug.Log(string.Format("Evaluation (JIT): {0:F2} | {1:F5} {2:F1}%", evalJit.TotalMilliseconds, evalJit.TotalMilliseconds / Iterations, evalJit.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
			Debug.Log(string.Format("Total (JIT): {0:F2} | {1:F5} {2:F1}%", totalJit.TotalMilliseconds, totalJit.TotalMilliseconds / Iterations, totalJit.TotalMilliseconds / totalJit.TotalMilliseconds * 100));
		}
		catch (Exception e)
		{
			Debug.LogError(e);
		}
	}


	/*
		Intel(R) Core(TM) i5-3570 CPU @ 3.40GHz

		Iterations: 100000

		Stage			 | Time(ms) | Time per Iteration(ms) | % of Total Time
		Tokenization	 | 23.99	| 0.00024				 | 0.0%
		Parsing			 | 18981.00 | 0.18981				 | 23.0%
		Binding			 | 52820.12 | 0.52820				 | 64.0%

		Compilation (JIT)| 10757.98 | 0.10758				 | 13.0%
		Evaluation (JIT) | 10.44	| 0.00010				 | 0.0%
		Total (JIT)		 | 82593.53 | 0.82594				 | 100.0% (base time)

		Compilation (AOT)| 12909.94 | 0.12910				 | 15.6%
		Evaluation (AOT) | 923.36	| 0.00923				 | 1.1%
		Total (AOT)		 | 85658.41 | 0.85658				 | 103.7%

		It took 0.82(1.02)ms to compile expressions into delegate
		And 0.00010(0.00923)ms to evaluate this expression
	*/
}

