﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Text;
using System.Windows.Media;

using ICSharpCode.Decompiler;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Tree Node representing a field, method, property, or event.
	/// </summary>
	public sealed class MethodTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly MethodDef method;

		public MethodDef MethodDefinition
		{
			get { return method; }
		}

		public MethodTreeNode(MethodDef method)
		{
			if (method == null)
				throw new ArgumentNullException("method");
			this.method = method;
		}

		public override object Text
		{
			get
			{
				return GetText(method, Language);
			}
		}

		public static object GetText(MethodDef method, Language language)
		{
			StringBuilder b = new StringBuilder();
			b.Append('(');
			for (int i = 0; i < method.Parameters.Count; i++) {
				if (method.Parameters[i].IsHiddenThisParameter)
					continue;
				if (method.Parameters[i].MethodSigIndex > 0)
					b.Append(", ");
				b.Append(language.TypeToString(method.Parameters[i].Type.ResolveGenericParams(method).ToTypeDefOrRef(), false, method.Parameters[i].ParamDef));
			}
			if (method.CallingConvention == CallingConvention.VarArg) {
				if (method.MethodSig.Params.Count > 0)
					b.Append(", ");
				b.Append("...");
			}
			b.Append(") : ");
			b.Append(language.TypeToString(method.ReturnType.ResolveGenericParams(method).ToTypeDefOrRef(), false, method.Parameters.ReturnParameter.ParamDef));
			return HighlightSearchMatch(method.Name, b.ToString());
		}

		public override object Icon
		{
			get { return GetIcon(method); }
		}

		public static ImageSource GetIcon(MethodDef method)
		{
			if (method.IsSpecialName && method.Name.String.StartsWith("op_", StringComparison.Ordinal)) {
				return Images.GetIcon(MemberIcon.Operator, GetOverlayIcon(method.Attributes), false);
			}

			if (method.IsStatic && method.HasCustomAttributes) {
				foreach (var ca in method.CustomAttributes) {
					if (ca.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute") {
						return Images.GetIcon(MemberIcon.ExtensionMethod, GetOverlayIcon(method.Attributes), false);
					}
				}
			}

			if (method.IsSpecialName &&
				(method.Name == ".ctor" || method.Name == ".cctor")) {
				return Images.GetIcon(MemberIcon.Constructor, GetOverlayIcon(method.Attributes), false);
			}

			if (method.HasImplMap)
				return Images.GetIcon(MemberIcon.PInvokeMethod, GetOverlayIcon(method.Attributes), true);

			bool showAsVirtual = method.IsVirtual && !(method.IsNewSlot && method.IsFinal) && !method.DeclaringType.IsInterface;

			return Images.GetIcon(
				showAsVirtual ? MemberIcon.VirtualMethod : MemberIcon.Method,
				GetOverlayIcon(method.Attributes),
				method.IsStatic);
		}

		private static AccessOverlayIcon GetOverlayIcon(MethodAttributes methodAttributes)
		{
			switch (methodAttributes & MethodAttributes.MemberAccessMask) {
				case MethodAttributes.Public:
					return AccessOverlayIcon.Public;
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return AccessOverlayIcon.Internal;
				case MethodAttributes.Family:
					return AccessOverlayIcon.Protected;
				case MethodAttributes.FamORAssem:
					return AccessOverlayIcon.ProtectedInternal;
				case MethodAttributes.Private:
					return AccessOverlayIcon.Private;
				case MethodAttributes.CompilerControlled:
					return AccessOverlayIcon.CompilerControlled;
				default:
					throw new NotSupportedException();
			}
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileMethod(method, output, options);
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			if (settings.SearchTermMatches(method.Name) && settings.Language.ShowMember(method))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		public override bool IsPublicAPI {
			get {
				return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
			}
		}
		
		IMemberRef IMemberTreeNode.Member
		{
			get { return method; }
		}
	}
}
