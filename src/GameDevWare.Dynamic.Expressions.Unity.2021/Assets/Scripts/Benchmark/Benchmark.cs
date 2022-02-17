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
	private bool isRunning;

	internal void OnGUI()
	{
		var width = 500.0f;
		var height = Screen.height - 40.0f;

		GUILayout.BeginArea(new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height));
		GUILayout.BeginVertical();

		GUI.enabled = !this.isRunning;
		if (GUILayout.Button("Measure Expression Performance"))
		{
			Debug.Log(SystemInfo.processorType);
			new Action(this.MeasureExpressionPerformance).BeginInvoke(null, null);
		}
		if (GUILayout.Button("Test Expression"))
		{
			const string expression = "(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\")";

			Debug.Log("Expression: " + expression);
			Debug.Log("Result: " + CSharpExpression.Evaluate<double>(expression));
		}
		GUI.enabled = true;

		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	private void MeasureExpressionPerformance()
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		this.isRunning = true;
		try
		{
			const string expression = "(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\")";

			Debug.Log("Expression: " + expression);
			Debug.Log("Iterations: " + Iterations);

			CSharpExpression.Evaluate<double>(expression);

			// tokenization
			var tokens = Tokenizer.Tokenize(expression);

			Debug.Log("Measuring tokenization...");
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				Tokenizer.Tokenize(expression);
			sw.Stop();
			var tokenization = sw.Elapsed;

			// parsing
			var parseTree = Parser.Parse(tokens);
			Debug.Log("Measuring parsing...");
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				Parser.Parse(tokens);
			sw.Stop();
			var parsing = sw.Elapsed;

			// binding
			var expressionBinder = new Binder(new ParameterExpression[0], typeof(double));
			var lambda = expressionBinder.Bind(parseTree.ToSyntaxTree(checkedScope: true));
			Debug.Log("Measuring binding...");
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				expressionBinder.Bind(parseTree.ToSyntaxTree(checkedScope: true));
			sw.Stop();
			var binding = sw.Elapsed;


			// compilation JIT
			var expressionLambda = (Expression<Func<double>>)lambda;
			var fnJit = expressionLambda.Compile();
			Debug.Log("Measuring JIT compilation...");
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
			{
				expressionLambda = (Expression<Func<double>>)lambda;
				expressionLambda.Compile();
			}
			sw.Stop();
			var compilationJit = sw.Elapsed;

			// compilation AOT
			expressionLambda = (Expression<Func<double>>)lambda;
			var fnAot = expressionLambda.CompileAot(forceAot: true);
			Debug.Log("Measuring AOT compilation...");
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
			{
				expressionLambda = (Expression<Func<double>>)lambda;
				expressionLambda.CompileAot(forceAot: true);
			}
			sw.Stop();
			var compilationAot = sw.Elapsed;

			// evaluation JIT
			fnAot();
			Debug.Log("Measuring JIT evaluation...");
			sw.Reset();
			sw.Start();
			for (var i = 0; i < Iterations; i++)
				fnAot();
			sw.Stop();
			var evalAot = sw.Elapsed;
			var totalAot = (tokenization + parsing + binding + compilationAot + evalAot);


			// evaluation AOT
			fnJit();
			Debug.Log("Measuring AOT evaluation...");
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
		finally
		{
			this.isRunning = false;
		}
	}


	/*
		Intel(R) Core(TM) i5-3570 CPU @ 3.40GHz

		Iterations: 100000
		Expression: (2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + Int32.Parse("10")

		// version 2.*

		It took 0.47ms (in AOT) to compile expressions into delegate
		And 0.00651ms (in AOT) to evaluate this expression

		Stage			 | Time(ms) | Time per Iteration(ms) | % of Total Time
		Tokenization: 	 | 46.07    | 0.00046                | 0.1%
		Parsing: 		 | 9306.90  | 0.09307                | 22.7%
		Binding:         | 22599.04 | 0.22599                | 55.0%

		Compilation (JIT)| 9117.12  | 0.09117                | 22.2%
		Evaluation (JIT) | 5.51     | 0.00006                | 0.0%
		Total (JIT)      | 41074.64 | 0.41075                | 100.0% (base time)

		Compilation (AOT)| 14457.30 | 0.14457                | 35.2%
		Evaluation (AOT) | 650.75   | 0.00651                | 1.6%
		Total (AOT)      | 47060.06 | 0.47060                | 114.6%


		version 1.*

		It took 0.85ms (in AOT) to compile expressions into delegate
		And 0.00923ms (in AOT) to evaluate this expression

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
	*/
}

