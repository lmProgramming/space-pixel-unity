﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastScriptReload.Scripts.Editor.Compilation.CodeRewriting
{
    public class RoslynUtils
    {
        public static string
            GetMemberFqdn(MemberDeclarationSyntax memberNode,
                string memberName) //TODO: try get rid of member name (needs to cast to whatever member it could be to get identifier)
        {
            var outer = GetMemberFqdnWithoutMemberName(memberNode);
            return !string.IsNullOrEmpty(outer)
                ? $"{outer}.{memberName}"
                : memberName;
        }

        public static string
            GetMemberFqdnWithoutMemberName(MemberDeclarationSyntax memberNode) //TODO: move out to helper class
        {
            var fullTypeContibutingAncestorNames = memberNode.Ancestors().OfType<MemberDeclarationSyntax>().Select(da =>
            {
                if (da is TypeDeclarationSyntax t) return t.Identifier.ToString();
                if (da is NamespaceDeclarationSyntax n) return n.Name.ToString();
                throw new Exception("Unable to resolve full field name");
            }).Reverse().ToList();

            return string.Join(".", fullTypeContibutingAncestorNames);
        }
    }
}