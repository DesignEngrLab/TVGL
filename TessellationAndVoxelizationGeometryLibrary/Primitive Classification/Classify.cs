// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-25-2016
// ***********************************************************************
// <copyright file="Primitive_Classification.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;
using TVGL.PrimitiveClassificationDetail;

namespace TVGL
{
    /// <summary>
    /// Class Primitive_Classification.
    /// </summary>
    public static class PrimitiveClassification
    {
        private static List<double> listOfLimitsABN, listOfLimitsMCM, listOfLimitsSM;
        private static List<List<int>> edgeRules, faceRules;
        private static double maxFaceArea;
        private static double primitivesBeforeFiltering;

        private static void InitializeFuzzinessRules()
        {
            listOfLimitsABN = Parameters.MakingListOfLimABNbeta2();
            listOfLimitsMCM = Parameters.MakingListOfLimMCMbeta2();
            listOfLimitsSM = Parameters.MakingListOfLimSMbeta2();
            edgeRules = Parameters.readingEdgesRules2();
            faceRules = Parameters.readingFacesRules();
            Debug.WriteLine("Edges and faces' rules have been read from the corresonding .csv files");
        }

        public static List<PrimitiveSurface> Run(TessellatedSolid ts)
        {
            if (listOfLimitsABN == null || listOfLimitsMCM == null | listOfLimitsSM == null || edgeRules == null || faceRules == null)
                InitializeFuzzinessRules();
            var allFacesWithScores = new List<FaceWithScores>(ts.Faces.Select(f => new FaceWithScores(f)));
            var allEdgeWithScores = new List<EdgeWithScores>(ts.Edges.Select(e => new EdgeWithScores(e)));
            var unassignedFaces = new HashSet<FaceWithScores>(allFacesWithScores);
            var unassignedEdges = new HashSet<EdgeWithScores>(allEdgeWithScores);
            var filteredOutEdges = new HashSet<EdgeWithScores>();  // Edges of the faces in dense area where both faces of the edge are dense
            var filteredOutFaces = new HashSet<FaceWithScores>();  // Edges of the faces in dense area where both faces of the edge are dense  

            // Filter out faces and edges 
            //SparseAndDenseClustering.Run(unassignedEdges, unassignedFaces, filteredOutEdges, filteredOutFaces);
            //FilterOutBadFaces(unassignedEdges, unassignedFaces, filteredOutEdges, filteredOutFaces);
            Debug.WriteLine("Filtering is complete.");
            // Classify Edges
            foreach (var e in unassignedEdges)
                EdgeFuzzyClassification(e);
            Debug.WriteLine("Edge classification is complete.");

            // Classify Faces
            foreach (var eachFace in unassignedFaces)
                FaceFuzzyClassification(eachFace, allEdgeWithScores);
            Debug.WriteLine("Face classification is complete.");

            //Now, for each face, take the combination with highest probability. 
            // UnassignedFaces.OrderByDescending(a => (a.FaceCat.Values.ToList()[0] - a.FaceCat.Values.ToList()[1]));
            //Can this really sort like what I want??? 
            foreach (var face in unassignedFaces)
            {
                face.CatToELDC = new Dictionary<PrimitiveSurfaceType, List<Edge>>();
                foreach (var cat in face.FaceCat.Keys)
                {
                    EdgesLeadToDesiredFaceCatFinder(face, cat, faceRules);
                }
            }
            var plannedSurfaces = new List<PlanningSurface>();
            maxFaceArea = unassignedFaces.Max(a => a.Face.Area);
            while (unassignedFaces.Count > 0)
            {
                Debug.WriteLine("# unassigned faces: " + unassignedFaces.Count);
                var topUnassignedFace = unassignedFaces.First();
                unassignedFaces.Remove(topUnassignedFace);
                var newSurfaces = groupFacesIntoPlanningSurfaces(topUnassignedFace, allFacesWithScores);
                plannedSurfaces.AddRange(DecideOnOverlappingPatches(newSurfaces, unassignedFaces));
            }
            var primitives = MakeSurfaces(plannedSurfaces.OrderByDescending(s => s.Metric).ToList());
            primitives = MinorCorrections(primitives, allEdgeWithScores);
            //PaintSurfaces(primitives, ts);
            //ReportStats(primitives);
            return primitives;
        }

        private static List<PrimitiveSurface> MinorCorrections(List<PrimitiveSurface> primitives, List<EdgeWithScores> allEdgeWithScores)
        {
            //foreach (var primitive in primitives.Where(a => a.Faces.Count == 1 && a is Sphere))
            primitivesBeforeFiltering = primitives.Count;
            //return primitives;
            for (var i = 0; i < primitives.Count - 1; i++)
            {
                var c = false;
                for (var j = i + 1; j < primitives.Count; j++)
                {
                    if ((primitives[i] is Cylinder /*|| primitives[i] is Cone*/) &&
                        primitives[j].Faces.Any(f => primitives[i].IsNewMemberOf(f)))
                    {
                        var del = new List<PolygonalFace>();
                        foreach (var f in primitives[j].Faces.Where(f => primitives[i].IsNewMemberOf(f)))
                        {
                            primitives[i].UpdateWith(f);
                            del.Add(f);
                        }
                        if (del.Count == primitives[j].Faces.Count)
                        {
                            primitives.RemoveAt(j);
                            j--;
                        }
                        else
                            foreach (var f in del)
                                primitives[j].Faces.Remove(f);
                        continue;
                    }
                    if ((primitives[j] is Cylinder /*|| primitives[j] is Cone*/) && primitives[i].Faces.Any(f => primitives[j].IsNewMemberOf(f)))
                    {
                        var del = new List<PolygonalFace>();
                        var cyl = (Cylinder)primitives[j];
                        // if the radius of the cylinder is very high, just continue;
                        var d = DistanceBetweenTwoVertices(primitives[j].Faces[0].Vertices[0].Position,
                            primitives[j].Faces[0].Vertices[1].Position);
                        if (Math.Abs(1 - (cyl.Radius - d) / (cyl.Radius)) < 0.001)
                            continue;
                        foreach (var f in primitives[i].Faces.Where(f => primitives[j].IsNewMemberOf(f)))
                        {
                            primitives[j].UpdateWith(f);
                            del.Add(f);
                        }
                        if (del.Count == primitives[i].Faces.Count)
                        {
                            primitives.RemoveAt(i);
                            c = true;
                            break;
                        }
                        else
                            foreach (var f in del)
                                primitives[i].Faces.Remove(f);
                    }
                }
                if (c) i--;
            }
            var flats = primitives.Where(p => p is Flat).Cast<Flat>().ToList();
            var cylinders = primitives.Where(p => p is Cylinder).Cast<Cylinder>().ToList();
            foreach (var cy in cylinders)
            {
                var d = DistanceBetweenTwoVertices(cy.Faces[0].Vertices[0].Position,
                    cy.Faces[0].Vertices[1].Position);
                if (Math.Abs(1 - (cy.Radius - d) / (cy.Radius)) < 0.001)
                    continue;
                foreach (var fl in flats)
                {
                    var newFaces = fl.Faces.Where(f => cy.IsNewMemberOf(f)).ToList();
                    if (newFaces.Any())
                    {
                        foreach (var f in newFaces)
                        {
                            cy.Faces.Add(f);
                            fl.Faces.Remove(f);
                        }
                    }
                    if (fl.Faces.Count == 0) primitives.Remove(fl);
                }
            }
            //foreach (var primitive in primitives.Where(a => a.Faces.Count == 1 && a is Sphere))
            primitivesBeforeFiltering = primitives.Count;
            for (var i = 0; i < primitives.Count; i++)
            {
                var primitive = primitives[i];

                //if (primitive.Faces.Count == 1 && primitive.GetType() == typeof(Sphere))
                //{
                //    primitives.Remove(primitive);
                //    i--;
                //}

                if (primitive.Faces.Count == 1 && primitive.GetType() == typeof(Sphere))
                {
                    var face = primitive.Faces[0];
                    var neighbors = new List<PrimitiveSurface>();
                    var catProbsOfEdges = face.Edges.Select(e => allEdgeWithScores.First(ews => ews.Edge == e).CatProb).ToList();
                    for (int j = 0; j < face.Edges.Count; j++)
                    {
                        if (!catProbsOfEdges[j].ContainsKey(500) && !catProbsOfEdges[j].ContainsKey(501) &&
                            !catProbsOfEdges[j].ContainsKey(502))
                            continue;
                        var edge = face.Edges[j];
                        var child = (edge.OwnedFace == face) ? edge.OtherFace : edge.OwnedFace;
                        // check and see which primitive has this child
                        foreach (
                            var otherPrimitive in
                            primitives.Where(a => a.Faces.Contains(child) && a.GetType() != typeof(Sphere)))
                        {
                            neighbors.Add(otherPrimitive);
                            break;
                        }
                    }
                    if (neighbors.Count == 0)
                    {
                        primitives.Remove(primitive);
                        i--;
                        continue;
                    }
                    var maxArea = neighbors.Max(a => a.Area);
                    var bestNeighbor = neighbors.Where(a => a.Area == maxArea).ToList()[0];
                    bestNeighbor.Faces.Add(face);
                    primitives.Remove(primitive);
                    i--;
                }
            }

            return primitives;
        }

        #region FilterOutBadFaces
        private static void FilterOutBadFaces(HashSet<EdgeWithScores> unassignedEdges, HashSet<FaceWithScores> unassignedFaces,
            HashSet<EdgeWithScores> filteredOutEdges, HashSet<FaceWithScores> filteredOutFaces)
        {
            var badFaces = unassignedFaces.Where(f => f.Face.Area < Parameters.Classifier_MinAreaForStartFace).ToList();
            foreach (var badFace in badFaces)
            {
                filteredOutFaces.Add(badFace);
                unassignedFaces.Remove(badFace);

            }
            var badEdges = unassignedEdges.Where(e => e.Edge.OtherFace == null || e.Edge.OwnedFace == null ||
                                                      (unassignedFaces.All(f => f.Face != e.Edge.OwnedFace) &&
                                                       unassignedFaces.All(f => f.Face != e.Edge.OtherFace))).ToList();
            foreach (var badEdge in badEdges)
            {
                filteredOutEdges.Add(badEdge);
                unassignedEdges.Remove(badEdge);
            }
        }


        #endregion
        #region Edge Classification
        private static void EdgeFuzzyClassification(EdgeWithScores e)
        {
            var ABN = AbnCalculator(e);
            var MCM = McmCalculator(e);
            var SM = SmCalculator(e);
            var ABNid = CatAndProbFinder(ABN, listOfLimitsABN);
            var MCMid = CatAndProbFinder(MCM, listOfLimitsMCM);
            var SMid = CatAndProbFinder(SM, listOfLimitsSM);
            if (ABNid.Count == 2 && ABNid[0].SequenceEqual(ABNid[1]))
                ABNid.RemoveAt(0);
            e.CatProb = new Dictionary<int, double>();
            foreach (var ABNprobs in ABNid)
            foreach (var MCMProbs in MCMid)
            foreach (var SMProbs in SMid)
            {
                double Prob;
                int group = EdgeClassifier2(ABNprobs, MCMProbs, SMProbs, edgeRules, out Prob);
                if (!e.CatProb.Keys.Contains(@group))
                    e.CatProb.Add(@group, Prob);
                else if (e.CatProb[@group] < Prob)
                    e.CatProb[@group] = Prob;
            }
        }


        internal static double AbnCalculator(EdgeWithScores eachEdge)
        {
            double ABN;
            if (eachEdge.Edge.InternalAngle <= Math.PI)
                ABN = (Math.PI - eachEdge.Edge.InternalAngle) * 180 / Math.PI;
            else ABN = eachEdge.Edge.InternalAngle * 180 / Math.PI;

            if (ABN >= 180)
                ABN -= 180;

            if (ABN > 179.5)
                ABN = 180 - ABN;

            if (Double.IsNaN(ABN))
            {
                var eee = eachEdge.Edge.OwnedFace.Normal.dotProduct(eachEdge.Edge.OtherFace.Normal);
                if (eee > 1)
                    eee = 1;
                ABN = Math.Acos(eee);
            }
            return ABN;
        }

        internal static double McmCalculator(EdgeWithScores eachEdge)
        {
            var cenMass1 = eachEdge.Edge.OwnedFace.Center;
            var cenMass2 = eachEdge.Edge.OtherFace.Center;
            var vector1 = new[] { cenMass1[0] - eachEdge.Edge.From.Position[0], cenMass1[1] - eachEdge.Edge.From.Position[1], cenMass1[2] - eachEdge.Edge.From.Position[2] };
            var vector2 = new[] { cenMass2[0] - eachEdge.Edge.From.Position[0], cenMass2[1] - eachEdge.Edge.From.Position[1], cenMass2[2] - eachEdge.Edge.From.Position[2] };
            var distance1 = eachEdge.Edge.Vector.normalize().dotProduct(vector1);
            var distance2 = eachEdge.Edge.Vector.normalize().dotProduct(vector2);
            //Mapped Center of Mass
            var MCM = (Math.Abs(distance1 - distance2)) / eachEdge.Edge.Length;
            return MCM;
        }

        internal static double SmCalculator(EdgeWithScores eachEdge)
        {
            var edgesOfFace1 = new List<Edge>(eachEdge.Edge.OwnedFace.Edges);
            var edgesOfFace2 = new List<Edge>(eachEdge.Edge.OtherFace.Edges);
            edgesOfFace1 = edgesOfFace1.OrderBy(a => a.Length).ToList();
            edgesOfFace2 = edgesOfFace2.OrderBy(a => a.Length).ToList();

            double smallArea, largeArea;
            if (eachEdge.Edge.OwnedFace.Area >= eachEdge.Edge.OtherFace.Area)
            {
                largeArea = eachEdge.Edge.OwnedFace.Area;
                smallArea = eachEdge.Edge.OtherFace.Area;
            }
            else
            {
                largeArea = eachEdge.Edge.OtherFace.Area;
                smallArea = eachEdge.Edge.OwnedFace.Area;
            }

            var r11 = edgesOfFace1[0].Length / edgesOfFace1[1].Length;
            var r12 = edgesOfFace1[0].Length / edgesOfFace1[2].Length;
            var r13 = edgesOfFace1[1].Length / edgesOfFace1[2].Length;
            var r21 = edgesOfFace2[0].Length / edgesOfFace2[1].Length;
            var r22 = edgesOfFace2[0].Length / edgesOfFace2[2].Length;
            var r23 = edgesOfFace2[1].Length / edgesOfFace2[2].Length;

            var similarity = Math.Abs(r11 - r21) + Math.Abs(r12 - r22) + Math.Abs(r13 - r23); // cannot exceed 3
            var areaSimilarity = 3 * Math.Abs(1 - (smallArea / largeArea));

            var SM = similarity + areaSimilarity;
            return SM;
        }


        private static int EdgeClassifier2(double[] ABNProbs, double[] MCMProbs, double[] SMProbs,
            List<List<int>> rulesArray, out double prob)
        {
            // go to the rules and and return an int corresponding to each region.
            // This function must be rewrited. It's crazy!!!!!!!!!
            prob = 0;
            var ABN = Convert.ToInt32(ABNProbs[0]);
            var MCM = Convert.ToInt32(MCMProbs[0]);
            var SM = Convert.ToInt32(SMProbs[0]);
            var t = 0;
            Boolean probabilityNotFound;
            do
            {
                probabilityNotFound = false;
                if (rulesArray[0][t] == ABN && rulesArray[1][t] == 10 && rulesArray[2][t] == 10)
                    prob = ABNProbs[1];
                else if (rulesArray[1][t] == MCM && rulesArray[0][t] == 10 && rulesArray[2][t] == 10)
                    prob = MCMProbs[1];
                else if (rulesArray[2][t] == SM && rulesArray[0][t] == 10 && rulesArray[1][t] == 10)
                    prob = SMProbs[1];
                else if (rulesArray[0][t] == ABN && rulesArray[1][t] == MCM && rulesArray[2][t] == 10)
                    prob = Math.Min(ABNProbs[1], MCMProbs[1]);
                else if (rulesArray[0][t] == ABN && rulesArray[1][t] == 10 && rulesArray[2][t] == SM)
                    prob = Math.Min(ABNProbs[1], SMProbs[1]);
                else if (rulesArray[0][t] == 10 && rulesArray[1][t] == MCM && rulesArray[2][t] == SM)
                    prob = Math.Min(MCMProbs[1], SMProbs[1]);
                else if (rulesArray[0][t] == ABN && rulesArray[1][t] == MCM && rulesArray[2][t] == SM)
                {
                    var m = Math.Min(ABNProbs[1], MCMProbs[1]);
                    prob = Math.Min(m, SMProbs[1]);
                }
                else probabilityNotFound = true;
            } while (probabilityNotFound && ++t < rulesArray[0].Count);
            if (probabilityNotFound) return 0;  // t would exceed the limit, so we return 0
            return rulesArray[3][t];
        }

        private static List<double[]> CatAndProbFinder(double metric, List<double> listOfLimits)
        {
            var CatAndProb = new List<double[]>();
            //Case 1
            if (metric <= listOfLimits[0])
            {
                CatAndProb.Add(new double[] { 0, 1 });
                return CatAndProb;
            }
            //Case 3
            if (metric >= listOfLimits[3] && metric <= listOfLimits[4])
            {
                CatAndProb.Add(new double[] { 1, 1 });
                return CatAndProb;
            }
            //Case 5
            if (metric >= listOfLimits[7])
            {
                CatAndProb.Add(new double[] { 2, 1 });
                return CatAndProb;
            }
            //Case 2
            if (metric > listOfLimits[0] && metric < listOfLimits[3])
            {
                CatAndProb = CatAndProbForCases2and4(listOfLimits[0], listOfLimits[1], listOfLimits[2], listOfLimits[3], metric, 2);
                return CatAndProb;
            }
            //Case 4
            if (metric > listOfLimits[4] && metric < listOfLimits[7])
            {
                CatAndProb = CatAndProbForCases2and4(listOfLimits[4], listOfLimits[5], listOfLimits[6], listOfLimits[7], metric, 4);
                return CatAndProb;
            }


            return CatAndProb;
        }

        private static List<double[]> CatAndProbForCases2and4(double p1, double p2, double p3, double p4, double metric, double Case)
        {
            var catAndProb = new List<double[]>();
            var prob1 = ((0 - 1) / (p2 - p1)) * (metric - p1) + 1;
            var prob2 = ((1 - 0) / (p4 - p3)) * (metric - p3) + 1;
            if (Case == 2)
            {
                catAndProb.Add(new[] { 0, prob1 });
                catAndProb.Add(new[] { 1, prob2 });
            }
            if (Case == 4)
            {
                catAndProb.Add(new[] { 1, prob1 });
                catAndProb.Add(new[] { 2, prob2 });
            }
            return catAndProb;
        }

        #endregion
        #region Face Classification
        private static void FaceFuzzyClassification(FaceWithScores eachFace, List<EdgeWithScores> allEdgeWithScores)
        {
            var c = 0;
            List<Dictionary<int, double>> t = eachFace.Face.Edges.Select(e => allEdgeWithScores.First(ews => ews.Edge == e).CatProb).ToList();
            eachFace.FaceCat = new Dictionary<PrimitiveSurfaceType, double>();
            eachFace.CatToCom = new Dictionary<PrimitiveSurfaceType, int[]>();
            eachFace.ComToEdge = new Dictionary<int[], Edge[]>();

            if (t[0] != null)
            {
                for (var i = 0; i < t[0].Count; i++)
                {
                    if (t[1] == null) break;
                    for (var j = 0; j < t[1].Count; j++)
                    {
                        if (t[2] == null) break;
                        for (var k = 0; k < t[2].Count; k++)
                        {
                            var combination = new[] { t[0].ToList()[i].Key, t[1].ToList()[j].Key, t[2].ToList()[k].Key };

                            var a = new double[3];
                            a[0] = t[0].ToList()[i].Value;
                            a[1] = t[1].ToList()[j].Value;
                            a[2] = t[2].ToList()[k].Value;

                            double totProb = 1;
                            var co = 0;

                            foreach (var ar in a.Where(p => p != 1))
                            {
                                totProb = totProb * (1 - ar);
                                co++;
                            }
                            if (co == 0)
                                totProb = 1;
                            else
                                totProb = 1 - totProb;

                            //var totProb = 1-((1-t[0].ToList()[i].Value) * (1-t[1].ToList()[j].Value) * (1-t[2].ToList()[k].Value));
                            //var combAndEdge = new Dictionary<int[], EdgeWithScores[]>();
                            //CanCom.Add(combination, totProb);

                            var edges = new[] { eachFace.Face.Edges[0], eachFace.Face.Edges[1], eachFace.Face.Edges[2] };
                            //combAndEdge.Add(combination.OrderBy(n=>n).ToArray(),edges);

                            var faceCat = FaceClassifier(combination, faceRules);

                            if (!eachFace.FaceCat.ContainsKey(faceCat))
                            {
                                eachFace.FaceCat.Add(faceCat, totProb);
                                eachFace.CatToCom.Add(faceCat, combination.OrderBy(n => n).ToArray());
                                eachFace.ComToEdge.Add(combination, edges);
                                eachFace.ComToEdge = sortingComToEdgeDic(eachFace.ComToEdge);
                                //eachFace.ComEdge.Add(faceCat, combAndEdge);
                            }
                            else
                            {
                                if (!(eachFace.FaceCat[faceCat] < totProb)) continue;
                                eachFace.FaceCat[faceCat] = totProb;
                                eachFace.ComToEdge.Remove(eachFace.CatToCom[faceCat]);
                                eachFace.CatToCom[faceCat] = combination.OrderBy(n => n).ToArray();
                                eachFace.ComToEdge.Add(combination, edges);
                                eachFace.ComToEdge = sortingComToEdgeDic(eachFace.ComToEdge);
                                //eachFace.ComEdge[faceCat] = combAndEdge;
                            }
                        }
                    }
                }
            }

        }

        private static PrimitiveSurfaceType FaceClassifier(int[] bestCombination, List<List<int>> faceRules)
        {
            var intToString = new Dictionary<int, PrimitiveSurfaceType>();
            intToString.Add(200, PrimitiveSurfaceType.Flat);
            intToString.Add(201, PrimitiveSurfaceType.Cylinder);
            intToString.Add(202, PrimitiveSurfaceType.Sphere);
            intToString.Add(203, PrimitiveSurfaceType.Flat_to_Curve);
            intToString.Add(204, PrimitiveSurfaceType.Dense);
            intToString.Add(205, PrimitiveSurfaceType.Unknown);
            var sortedCom = bestCombination.OrderBy(n => n).ToArray();
            for (var i = 0; i < faceRules[0].Count; i++)
            {
                var arrayOfRule = new[] { faceRules[0][i], faceRules[1][i], faceRules[2][i] };
                var sortedAofR = arrayOfRule.OrderBy(n => n).ToArray();
                if (sortedCom[0] == sortedAofR[0] && sortedCom[1] == sortedAofR[1] && sortedCom[2] == sortedAofR[2])
                    return intToString[faceRules[3][i]];
            }
            return PrimitiveSurfaceType.Unknown;
        }

        private static Dictionary<int[], Edge[]> sortingComToEdgeDic(Dictionary<int[], Edge[]> d)
        {
            var lastAdded = d.ToList()[d.Keys.Count - 1];
            var k = lastAdded.Key;
            var v = lastAdded.Value;
            d.Remove(k);
            if (k[0] > k[1])
            {
                var a = k[0];
                k[0] = k[1];
                k[1] = a;
                var b = v[0];
                v[0] = v[1];
                v[1] = b;
            }
            if (k[1] > k[2])
            {
                var a = k[1];
                k[1] = k[2];
                k[2] = a;
                var b = v[1];
                v[1] = v[2];
                v[2] = b;
            }
            if (k[0] > k[1])
            {
                var a = k[0];
                k[0] = k[1];
                k[1] = a;
                var b = v[0];
                v[0] = v[1];
                v[1] = b;
            }
            d.Add(k, v);
            return d;
        }

        #endregion
        #region EdgesLeadToDesiredFaceCatFinder
        private static void EdgesLeadToDesiredFaceCatFinder(FaceWithScores face, PrimitiveSurfaceType p, List<List<int>> faceRules)
        {
            // This function takes a face and a possible category and returns 
            // edges which lead to that category
            // We need to define some rules. for example, if the p is  F, with
            // combination of F F SE, then return edges of F and F. 
            //eltc = [faceRules[4][i],faceRules[5][i],faceRules[6][i]];
            var edges = new List<Edge>();
            var com = face.CatToCom[p];
            var edgesFD = new Edge[3];

            /////////////////////////////////////////////////////////////////////
            // Checking the equality of 2 arrays: com and comb
            foreach (var comb in face.ComToEdge.Keys)
            {
                var counter = 0;
                var list = new List<int>();
                for (var m = 0; m < 3; m++)
                {
                    for (var n = 0; n < 3; n++)
                    {
                        if (!list.Contains(n))
                        {
                            if (com[m] == comb[n])
                            {
                                counter++;
                                list.Add(n);
                                break;
                            }
                        }
                    }
                }
                bool equals = counter == 3;
                if (@equals)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        edgesFD[i] = face.ComToEdge[comb][i];
                    }
                }
            }
            /////////////////////////////////////////////////////////////////////

            for (var i = 0; i < faceRules[0].Count; i++)
            {
                var arrayOfRule = new[] { faceRules[0][i], faceRules[1][i], faceRules[2][i] };
                /*var q = from a in com
                        join b in arrayOfRule on a equals b
                        select a;
                bool equals = com.Length == arrayOfRule.Length && q.Count() == com.Length;*/

                // Checking the equality of 2 arrays:com and arrayOfRules
                var counter = 0;
                var list = new List<int>();
                for (var m = 0; m < 3; m++)
                {
                    for (var n = 0; n < 3; n++)
                    {
                        if (!list.Contains(n))
                        {
                            if (com[m] == arrayOfRule[n])
                            {
                                counter++;
                                list.Add(n);
                                break;
                            }
                        }
                    }
                }
                bool equals = counter == 3;
                arrayOfRule.OrderBy(n => n).ToArray();
                if (@equals)
                {
                    var EdgesLead = new[] { faceRules[4][i], faceRules[5][i], faceRules[6][i] };
                    var SortedEL = EdgesLead.OrderBy(n => n).ToArray();
                    for (var t = 0; t < 3; t++)
                        //foreach (var EL in SortedEL.Where(a => a != 1000))
                    {
                        if (SortedEL[t] == 1000) continue;
                        //var index = Array.IndexOf(SortedEL, EL);
                        edges.Add(edgesFD[t]);
                    }
                    break;
                    //return edges;
                }
            }
            face.CatToELDC.Add(p, edges);
            //return null;
        }
        #endregion
        #region Group Faces Into Primitives
        private static List<PlanningSurface> groupFacesIntoPlanningSurfaces(FaceWithScores seedFace, List<FaceWithScores> allFacesWithScores)
        {
            // candidatePatches are where we store successful surfaces that start on the stackOfPotentialPrimitives
            // they are collected here and returned to the main classification method
            var candidatePatches = new List<PlanningSurface>();
            // the outer depthfirst search builds stackOfPotentialPatches, which acts as seeds to the inner DFS.
            // While on this stack, all surfaces ONLY have one face - which is the start to
            // the inner DFS. Once an inner DFS ends, its successful result is added to candidatePatches.
            var stackOfPotentialPatches = new Stack<PlanningSurface>();
            // put possible start states on the stack starting with the seedface. But don't start with faces that
            // are too small or have not
            //if (seedFace.Face.Area < Parameters.Classifier_MinAreaForStartFace || seedFace.CatToCom.Count == 0)
            //    return new List<PlanningSurface>();
            foreach (var faceCat in seedFace.FaceCat.Keys)
                stackOfPotentialPatches.Push(new PlanningSurface(faceCat, seedFace));
            while (stackOfPotentialPatches.Any())
            {
                var primitive = stackOfPotentialPatches.Pop();
                var type = primitive.SurfaceType;
                if (AlreadySearchedPrimitive(primitive, candidatePatches)) continue;

                // start new depth first search on this primitive
                var innerStack = new Stack<FaceWithScores>();
                innerStack.Push(primitive.Faces[0]);
                while (innerStack.Any())
                {
                    var openBranchFace = innerStack.Pop();
                    foreach (var eachEdge in openBranchFace.CatToELDC[type])
                    {
                        var child = (eachEdge.OwnedFace == openBranchFace.Face)
                            ? allFacesWithScores.First(f => f.Face == eachEdge.OtherFace)
                            : allFacesWithScores.First(f => f.Face == eachEdge.OwnedFace);
                        if (primitive.Faces.Contains(child)) continue;
                        if (child.FaceCat == null) continue; // you've moved onto a dense or bad face
                        if (!child.CatToCom.ContainsKey(type)) continue;
                        if (child.FaceCat.ContainsKey(type))
                        {
                            child.CatToCom.Remove(type);
                            primitive.Add(child);
                            innerStack.Push(child);
                        }
                        // else
                        foreach (var surfaceType in child.FaceCat.Keys
                            .Where(surfaceType => !stackOfPotentialPatches.Any(p => p.SurfaceType == surfaceType
                                                                                    && p.Faces[0] == child)))
                            stackOfPotentialPatches.Push(new PlanningSurface(surfaceType, child));
                    }
                }
                candidatePatches.Add(primitive);
            }
            Debug.WriteLine("new patches: " + candidatePatches.Count + " -- comprised of #faces: " + candidatePatches.SelectMany(cp => cp.Faces).Distinct().Count());
            return candidatePatches;
        }

        private static bool AlreadySearchedPrimitive(PlanningSurface newSeed, List<PlanningSurface> candidatePatches)
        {
            return (candidatePatches.Any(p => p.SurfaceType == newSeed.SurfaceType
                                              && p.Faces.Contains(newSeed.Faces[0])));
        }

        public static double DistanceBetweenTwoVertices(double[] vertex1, double[] vertex2)
        {
            return
                Math.Sqrt((Math.Pow(vertex1[0] - vertex2[0], 2)) +
                          (Math.Pow(vertex1[1] - vertex2[1], 2)) +
                          (Math.Pow(vertex1[2] - vertex2[2], 2)));
        }

        #endregion
        #region Decide In Overlapping Patches
        private static IEnumerable<PlanningSurface> DecideOnOverlappingPatches(List<PlanningSurface> surfaces,
            HashSet<FaceWithScores> unassignedFaces)
        {
            var orderedSurfaces = surfaces.OrderByDescending(s => s.Metric).ToList();
            var completeSurfaces = new List<PlanningSurface>();
            while (orderedSurfaces.Any())
            {
                var surface = orderedSurfaces[0];
                orderedSurfaces.RemoveAt(0);
                if (surface.Faces.Count < 1)
                {
                    //if (surface.Faces.Count == 1) unassignedFaces.Remove(surface.Faces[0]);
                    continue;
                }
                completeSurfaces.Add(surface);
                var otherSurfsThatShareFaces = new List<PlanningSurface>();
                foreach (var f in surface.Faces)
                {
                    unassignedFaces.Remove(f);
                    for (int j = orderedSurfaces.Count - 1; j >= 0; j--)
                    {
                        var otherSurface = orderedSurfaces[j];
                        if (otherSurface.Faces.Contains(f))
                        {
                            otherSurfsThatShareFaces.Add(otherSurface);
                            otherSurface.Remove(f);
                            orderedSurfaces.RemoveAt(j);
                        }
                    }
                }
                foreach (var reducedSurface in otherSurfsThatShareFaces)
                    ReInsert(reducedSurface, orderedSurfaces);
            }
            return completeSurfaces;
        }

        private static void ReInsert(PlanningSurface surface, List<PlanningSurface> orderedPrimitives)
        {
            int endIndex = orderedPrimitives.Count;
            if (endIndex == 0)
            {
                orderedPrimitives.Add(surface);
                return;
            }
            int startIndex = 0;
            var midIndex = endIndex / 2;
            do
            {
                if (surface.Metric > orderedPrimitives[midIndex].Metric)
                    endIndex = midIndex;
                else startIndex = midIndex;
                midIndex = startIndex + (endIndex - startIndex) / 2;
            } while (midIndex != endIndex && midIndex != startIndex);
            orderedPrimitives.Insert(midIndex, surface);
        }

        #endregion
        #region Make Primitives
        private static List<PrimitiveSurface> MakeSurfaces(List<PlanningSurface> plannedSurfaces)
        {
            var completeSurfaces = new List<PrimitiveSurface>();

            while (plannedSurfaces.Any())
            {
                Debug.WriteLine("# primitives to make: " + plannedSurfaces.Count);
                var topPlannedSurface = plannedSurfaces[0];
                plannedSurfaces.RemoveAt(0);
                if (topPlannedSurface.Faces.Count < 1)
                    continue;
                if (topPlannedSurface.Faces.Count == 1)
                {
                    var face = topPlannedSurface.Faces[0].Face;
                    if (face.Area < maxFaceArea / 9)
                    {
                        completeSurfaces.Add(new Sphere(topPlannedSurface.Faces.Select(f => f.Face)));
                        continue;
                    }
                    completeSurfaces.Add(new Flat(topPlannedSurface.Faces.Select(f => f.Face)));
                    continue;
                }
                var topPrimitiveSurface = CreatePrimitiveSurface(topPlannedSurface);
                for (var i = plannedSurfaces.Count - 1; i >= 0; i--)
                {
                    if (plannedSurfaces[i].SurfaceType == topPlannedSurface.SurfaceType
                        && plannedSurfaces[i].Faces.All(f => topPrimitiveSurface.IsNewMemberOf(f.Face)))
                    {
                        foreach (var face in plannedSurfaces[i].Faces)
                            topPrimitiveSurface.UpdateWith(face.Face);
                        plannedSurfaces.RemoveAt(i);
                    }
                }
                completeSurfaces.Add(topPrimitiveSurface);
            }
            return completeSurfaces;
        }

        private static PrimitiveSurface CreatePrimitiveSurface(PlanningSurface topPlannedSurface)
        {
            var surfaceType = topPlannedSurface.SurfaceType;
            var faces = new List<PolygonalFace>(topPlannedSurface.Faces.Select(f => f.Face));

            switch (surfaceType)
            {
                case PrimitiveSurfaceType.Flat:
                    return new Flat(faces);
                case PrimitiveSurfaceType.Cylinder:
                    double[] axis;
                    double coneAngle;
                    if (IsReallyACone(faces, out axis, out coneAngle))
                        return new Cone(faces, axis, coneAngle);
                    if (IsReallyAFlat(faces)) return new Flat(faces);
                    return new Cylinder(faces, axis);
                case PrimitiveSurfaceType.Sphere:
                    if (IsReallyATorus(faces))
                        return new Torus(faces);
                    return new Sphere(faces);
                default: throw new Exception("Cannot build Create Primitive Surface of type: " + surfaceType);
            }
        }

        private static bool IsReallyAFlat(IEnumerable<PolygonalFace> faces)
        {
            return (ListFunctions.FacesWithDistinctNormals(faces.ToList()).Count == 1);
        }

        private static bool IsReallyACone(IEnumerable<PolygonalFace> facesAll, out double[] axis, out double coneAngle)
        {
            var faces = ListFunctions.FacesWithDistinctNormals(facesAll.ToList());
            var n = faces.Count;
            if (faces.Count <= 1)
            {
                axis = null;
                coneAngle = double.NaN;
                return false;
            }
            if (faces.Count == 2)
            {
                axis = faces[0].Normal.crossProduct(faces[1].Normal).normalize();
                coneAngle = 0.0;
                return false;
            }

            // a simpler approach: if the cross product of the normals are all parallel, it's a cylinder,
            // otherwise, cone.

            /*var r = new Random();
            var rndList = new List<int>();
            var crossProd = new List<double[]>();
            var c = 0;
            while (c < 20)
            {
                var ne = r.Next(facesAll.Count);
                if (!rndList.Contains(ne)) rndList.Add(ne);
                c++;
            }
            for (var i = 0; i < rndList.Count-1; i++)
            {
                for (var j = i + 1; j < rndList.Count; j++)
                {
                    crossProd.Add(facesAll[i].Normal.crossProduct(facesAll[j].Normal).normalize());
                }
            }
            axis = crossProd[0];
            coneAngle = 0.0;
            for (var i = 0; i < crossProd.Count - 1; i++)
            {
                for (var j = i + 1; j < crossProd.Count; j++)
                {
                    if (Math.Abs(crossProd[i].dotProduct(crossProd[j]) - 1) < 0.00000008) return true;
                }
            }
            return false;*/

            // find the plane that the normals live on in the Gauss sphere. If it is not
            // centered at 0 then you have a cone.
            // since the vectors that are the difference of two normals (v = n1 - n2) would
            // be in the plane, let's first figure out the average plane of this normal
            var inPlaneVectors = new double[n][];
            inPlaneVectors[0] = faces[0].Normal.subtract(faces[n - 1].Normal);
            for (int i = 1; i < n; i++)
                inPlaneVectors[i] = faces[i].Normal.subtract(faces[i - 1].Normal);

            var normalsOfGaussPlane = new List<double[]>();
            var tempCross = inPlaneVectors[0].crossProduct(inPlaneVectors[n - 1]).normalize();
            if (!tempCross.Any(double.IsNaN))
                normalsOfGaussPlane.Add(tempCross);
            for (int i = 1; i < n; i++)
            {
                tempCross = inPlaneVectors[i].crossProduct(inPlaneVectors[i - 1]).normalize();
                if (!tempCross.Any(double.IsNaN))
                    if (tempCross.dotProduct(normalsOfGaussPlane[0]) >= 0)
                        normalsOfGaussPlane.Add(tempCross);
                    else normalsOfGaussPlane.Add(tempCross.multiply(-1));
            }
            var normalOfGaussPlane = new double[3];
            normalOfGaussPlane = normalsOfGaussPlane.Aggregate(normalOfGaussPlane, (current, c) => current.add(c));
            normalOfGaussPlane = normalOfGaussPlane.divide(normalsOfGaussPlane.Count);

            var distance = faces.Sum(face => face.Normal.dotProduct(normalOfGaussPlane));
            if (distance < 0)
            {
                axis = normalOfGaussPlane.multiply(-1);
                distance = -distance / n;
            }
            else
            {
                distance /= n;
                axis = normalOfGaussPlane;
            }
            coneAngle = Math.Asin(distance);
            return (Math.Abs(distance) >= Parameters.MinConeGaussPlaneOffset);
        }

        private static bool IsReallyATorus(IEnumerable<PolygonalFace> faces)
        {
            return false;
            throw new NotImplementedException();
        }

        #endregion
        #region Show Results
        private static void PaintSurfaces(List<PrimitiveSurface> primitives, TessellatedSolid ts)
        {
            foreach (var f in ts.Faces)
            {
                f.Color = new Color(KnownColors.Yellow);
            }
            var i = 0;
            foreach (var primitiveSurface in primitives)
            {
                if (primitiveSurface is Cylinder)
                {
                    i++;
                    if (i == 5)
                    {
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.Red);
                    }
                }

                if (primitiveSurface is Cone)
                    foreach (var f in primitiveSurface.Faces)
                        f.Color = new Color(KnownColors.Pink);
                else if (primitiveSurface is Sphere)
                    foreach (var f in primitiveSurface.Faces)
                        f.Color = new Color(KnownColors.Blue);
                else if (primitiveSurface is Flat)
                    foreach (var f in primitiveSurface.Faces)
                        f.Color = new Color(KnownColors.Green);
                else if (primitiveSurface is DenseRegion)
                    foreach (var f in primitiveSurface.Faces)
                        f.Color = new Color(KnownColors.Black);
            }
        }
        private static void ReportStats(List<PrimitiveSurface> primitives)
        {
            Debug.WriteLine("**************** RESULTS *******************");
            Debug.WriteLine("Number of Primitives = " + primitives.Count);
            Debug.WriteLine("Number of Primitives Before Filtering= " + primitivesBeforeFiltering);
            Debug.WriteLine("Number of Flats = " + primitives.Count(p => p is Flat));
            Debug.WriteLine("Number of Cones = " + primitives.Count(p => p is Cone));
            Debug.WriteLine("Number of Cylinders = " + primitives.Count(p => p is Cylinder));
            Debug.WriteLine("Number of Spheres = " + primitives.Count(p => p is Sphere));
            Debug.WriteLine("Number of Torii = " + primitives.Count(p => p is Torus));
            Debug.WriteLine("Primitive Max Faces = " + primitives.Max(p => p.Faces.Count));
            var minFaces = primitives.Min(p => p.Faces.Count);
            Debug.WriteLine("Primitive Min Faces = " + minFaces);
            Debug.WriteLine("Number of with min faces = " + primitives.Count(p => p.Faces.Count == minFaces));
            Debug.WriteLine("Primitive Avg. Faces = " + primitives.Average(p => p.Faces.Count));
        }
        #endregion

    }

    /// <summary>
    /// Enum PrimitiveSurfaceType
    /// </summary>
    public enum PrimitiveSurfaceType
    {
        /// <summary>
        /// The unknown
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The dense
        /// </summary>
        Dense = 123456789,
        /// <summary>
        /// The flat
        /// </summary>
        Flat = 500,
        /// <summary>
        /// The cylinder
        /// </summary>
        Cylinder = 501,
        /// <summary>
        /// The sphere
        /// </summary>
        Sphere = 502,
        /// <summary>
        /// The flat_to_ curve
        /// </summary>
        Flat_to_Curve = 503,
        /// <summary>
        /// The sharp
        /// </summary>
        Sharp = 504,
        /// <summary>
        /// The cone
        /// </summary>
        Cone = 1
    }
}