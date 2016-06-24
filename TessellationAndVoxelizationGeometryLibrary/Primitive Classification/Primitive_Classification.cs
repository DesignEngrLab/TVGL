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
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// Class Primitive_Classification.
    /// </summary>
    public static partial class Primitive_Classification
    {
        /// <summary>
        /// The list of limits abn
        /// </summary>
        private static double[] listOfLimitsABN, listOfLimitsMCM, listOfLimitsSM;
        /// <summary>
        /// The edge rules
        /// </summary>
        private static int[,] edgeRules, faceRules;

        /// <summary>
        /// Initializes the fuzziness rules.
        /// </summary>
        private static void InitializeFuzzinessRules()
        {
            listOfLimitsABN = ClassificationConstants.MakingListOfLimABNbeta2();
            listOfLimitsMCM = ClassificationConstants.MakingListOfLimMCMbeta2();
            listOfLimitsSM = ClassificationConstants.MakingListOfLimSMbeta2();
            edgeRules = ClassificationConstants.readingEdgesRules2();
            faceRules = ClassificationConstants.readingFacesRules();
            Message.output("Edges and faces' rules have been read from the corresponding .csv files", 4);
        }

        /// <summary>
        /// Runs the specified tessellated solid through the primitive classification method.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>List&lt;PrimitiveSurface&gt;.</returns>
        public static List<PrimitiveSurface> Run(TessellatedSolid ts)
        {
            if (listOfLimitsABN == null || listOfLimitsMCM == null | listOfLimitsSM == null || edgeRules == null ||
                faceRules == null)
                InitializeFuzzinessRules();
            var allFacesWithScores = new List<FaceWithScores>(ts.Faces.Select(f => new FaceWithScores(f)));
            var allEdgeWithScores = new List<EdgeWithScores>(ts.Edges.Select(e => new EdgeWithScores(e)));
            var unassignedFaces = new HashSet<FaceWithScores>(allFacesWithScores);
            var unassignedEdges = new HashSet<EdgeWithScores>(allEdgeWithScores);
            var filteredOutEdges = new HashSet<EdgeWithScores>();
                // Edges of the faces in dense area where both faces of the edge are dense
            var filteredOutFaces = new HashSet<FaceWithScores>();
                // Edges of the faces in dense area where both faces of the edge are dense  

            /*foreach (var eachFace in unassignedFaces)
            {
                foreach (var edge in eachFace.Face.Edges)
                {
                    if (!allEdgeWithScores.Select(e => e.Edge).Contains(edge))
                    {
                        allEdgeWithScores.Add(new EdgeWithScores(edge));
                        unassignedEdges.Add(new EdgeWithScores(edge));
                    }
                }
            }*/

            // Filter out faces and edges 
            //SparseAndDenseClustering.Run(unassignedEdges, unassignedFaces, filteredOutEdges, filteredOutFaces);
            FilterOutBadFaces(unassignedEdges, unassignedFaces, filteredOutEdges, filteredOutFaces);
            Message.output("Filtering is complete.", 5);

            // Classify Edges
            foreach (var e in unassignedEdges)
                EdgeFuzzyClassification(e);
            Message.output("Edge classification is complete.", 5);

            // Classify Faces
            //unassignedFaces.RemoveWhere(f => f.Face.Edges.Count < 3);
            foreach (var eachFace in unassignedFaces)
                FaceFuzzyClassification(eachFace, allEdgeWithScores);
            Message.output("Face classification is complete.", 5);

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
            var maxFaceArea = unassignedFaces.Max(a => a.Face.Area);
            while (unassignedFaces.Count > 0)
            {
                Message.output("# unassigned faces: " + unassignedFaces.Count, 5);
                var topUnassignedFace = unassignedFaces.First();
                unassignedFaces.Remove(topUnassignedFace);
                var newSurfaces = groupFacesIntoPlanningSurfaces(topUnassignedFace, allFacesWithScores);
                plannedSurfaces.AddRange(DecideOnOverlappingPatches(newSurfaces, unassignedFaces));
            }
            var primitives = MakeSurfaces(plannedSurfaces.OrderByDescending(s => s.Metric).ToList(), maxFaceArea);
            var primitivesBeforeFiltering = primitives.Count;
            primitives = MinorCorrections(primitives, allEdgeWithScores);
            PaintSurfaces(primitives, ts);
            ReportStats(primitives, primitivesBeforeFiltering);
            return primitives;
        }

        /// <summary>
        /// Minors the corrections.
        /// </summary>
        /// <param name="primitives">The primitives.</param>
        /// <param name="allEdgeWithScores">All edge with scores.</param>
        /// <returns>List&lt;PrimitiveSurface&gt;.</returns>
        private static List<PrimitiveSurface> MinorCorrections(List<PrimitiveSurface> primitives,
            List<EdgeWithScores> allEdgeWithScores)
        {
            //foreach (var primitive in primitives.Where(a => a.Faces.Count == 1 && a is Sphere))
            for (var i = 0; i < primitives.Count; i++)
            {
                var primitive = primitives[i];

                //if (primitive.Faces.Count == 1 && primitive.GetType() == typeof(Sphere))
                //{
                //    primitives.Remove(primitive);
                //    i--;
                //}

                if (primitive.Faces.Count == 1 && primitive.GetType() == typeof (Sphere))
                {
                    var face = primitive.Faces[0];
                    var neighbors = new List<PrimitiveSurface>();
                    var catProbsOfEdges =
                        face.Edges.Select(e => allEdgeWithScores.First(ews => ews.Edge == e).CatProb).ToList();
                    for (var j = 0; j < face.Edges.Count; j++)
                    {
                        if (!catProbsOfEdges[j].ContainsKey(500) && !catProbsOfEdges[j].ContainsKey(501) &&
                            !catProbsOfEdges[j].ContainsKey(502))
                            continue;
                        var edge = face.Edges[j];
                        var child = edge.OwnedFace == face ? edge.OtherFace : edge.OwnedFace;
                        // check and see which primitive has this child
                        foreach (
                            var otherPrimitive in
                                primitives.Where(a => a.Faces.Contains(child) && a.GetType() != typeof (Sphere)))
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

        /// <summary>
        /// Filters the out bad faces.
        /// </summary>
        /// <param name="unassignedEdges">The unassigned edges.</param>
        /// <param name="unassignedFaces">The unassigned faces.</param>
        /// <param name="filteredOutEdges">The filtered out edges.</param>
        /// <param name="filteredOutFaces">The filtered out faces.</param>
        private static void FilterOutBadFaces(HashSet<EdgeWithScores> unassignedEdges,
            HashSet<FaceWithScores> unassignedFaces,
            HashSet<EdgeWithScores> filteredOutEdges, HashSet<FaceWithScores> filteredOutFaces)
        {
            var badFaces =
                unassignedFaces.Where(f => f.Face.Area < ClassificationConstants.Classifier_MinAreaForStartFace)
                    .ToList();
            foreach (var badFace in badFaces)
            {
                filteredOutFaces.Add(badFace);
                unassignedFaces.Remove(badFace);
            }
            var badEdges = unassignedEdges.Where(e => e.Edge.OtherFace == null || e.Edge.OwnedFace == null /* ||
                                                             (unassignedFaces.All(f => f.Face != e.Edge.OwnedFace) &&
                                                              unassignedFaces.All(f => f.Face != e.Edge.OtherFace))*/)
                .ToList();
            foreach (var badEdge in badEdges)
            {
                filteredOutEdges.Add(badEdge);
                unassignedEdges.Remove(badEdge);
            }
        }

        #endregion

        #region EdgesLeadToDesiredFaceCatFinder

        /// <summary>
        /// Edgeses the lead to desired face cat finder.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="p">The p.</param>
        /// <param name="faceRules">The face rules.</param>
        private static void EdgesLeadToDesiredFaceCatFinder(FaceWithScores face, PrimitiveSurfaceType p,
            int[,] faceRules)
        {
            // This function takes a face and a possible category and returns 
            // edges which lead to that category
            // We need to define some rules. for example, if the p is  F, with
            // combination of F F SE, then return edges of F and F. 
            //eltc = [faceRules[4,i],faceRules[5,i],faceRules[6,i]];
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
                var equals = counter == 3;
                if (@equals)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        edgesFD[i] = face.ComToEdge[comb][i];
                    }
                }
            }
            /////////////////////////////////////////////////////////////////////

            for (var i = 0; i < faceRules.GetLength(1); i++)
            {
                var arrayOfRule = new[] {faceRules[0, i], faceRules[1, i], faceRules[2, i]};
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
                var equals = counter == 3;
                arrayOfRule.OrderBy(n => n).ToArray();
                if (@equals)
                {
                    var EdgesLead = new[] {faceRules[4, i], faceRules[5, i], faceRules[6, i]};
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

        #region Edge Classification

        /// <summary>
        /// Edges the fuzzy classification.
        /// </summary>
        /// <param name="e">The e.</param>
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
                        var group = EdgeClassifier2(ABNprobs, MCMProbs, SMProbs, edgeRules, out Prob);
                        if (!e.CatProb.Keys.Contains(@group))
                            e.CatProb.Add(@group, Prob);
                        else if (e.CatProb[@group] < Prob)
                            e.CatProb[@group] = Prob;
                    }
        }


        /// <summary>
        /// Abns the calculator.
        /// </summary>
        /// <param name="eachEdge">The each edge.</param>
        /// <returns>System.Double.</returns>
        internal static double AbnCalculator(EdgeWithScores eachEdge)
        {
            double ABN;
            if (eachEdge.Edge.InternalAngle <= Math.PI)
                ABN = (Math.PI - eachEdge.Edge.InternalAngle)*180/Math.PI;
            else ABN = eachEdge.Edge.InternalAngle*180/Math.PI;

            if (ABN >= 180)
                ABN -= 180;

            if (ABN > 179.5)
                ABN = 180 - ABN;

            if (double.IsNaN(ABN))
            {
                var eee = eachEdge.Edge.OwnedFace.Normal.dotProduct(eachEdge.Edge.OtherFace.Normal);
                if (eee > 1)
                    eee = 1;
                ABN = Math.Acos(eee);
            }
            return ABN;
        }

        /// <summary>
        /// MCMs the calculator.
        /// </summary>
        /// <param name="eachEdge">The each edge.</param>
        /// <returns>System.Double.</returns>
        internal static double McmCalculator(EdgeWithScores eachEdge)
        {
            var cenMass1 = eachEdge.Edge.OwnedFace.Center;
            var cenMass2 = eachEdge.Edge.OtherFace.Center;
            var vector1 = new[]
            {
                cenMass1[0] - eachEdge.Edge.From.Position[0], cenMass1[1] - eachEdge.Edge.From.Position[1],
                cenMass1[2] - eachEdge.Edge.From.Position[2]
            };
            var vector2 = new[]
            {
                cenMass2[0] - eachEdge.Edge.From.Position[0], cenMass2[1] - eachEdge.Edge.From.Position[1],
                cenMass2[2] - eachEdge.Edge.From.Position[2]
            };
            var distance1 = eachEdge.Edge.Vector.normalize().dotProduct(vector1);
            var distance2 = eachEdge.Edge.Vector.normalize().dotProduct(vector2);
            //Mapped Center of Mass
            var MCM = Math.Abs(distance1 - distance2)/eachEdge.Edge.Length;
            return MCM;
        }

        /// <summary>
        /// Sms the calculator.
        /// </summary>
        /// <param name="eachEdge">The each edge.</param>
        /// <returns>System.Double.</returns>
        internal static double SmCalculator(EdgeWithScores eachEdge)
        {
            //var edgesOfFace1 = new List<Edge>(eachEdge.Edge.OwnedFace.Edges);
            var edgesOfFace1Length = eachEdge.Edge.OwnedFace.Edges.Select(e => e.Length).ToList();
            if (edgesOfFace1Length.Count < 3)
            {
                // find the missing edge and add its length
                edgesOfFace1Length.Add(AddMissingEdgeLength(eachEdge.Edge.OwnedFace.Edges));
            }
            edgesOfFace1Length.Sort();
            //var edgesOfFace2 = new List<Edge>(eachEdge.Edge.OtherFace.Edges);
            var edgesOfFace2Length = eachEdge.Edge.OtherFace.Edges.Select(e => e.Length).ToList();
            if (edgesOfFace2Length.Count < 3)
            {
                // find the missing edge and add its length
                edgesOfFace2Length.Add(AddMissingEdgeLength(eachEdge.Edge.OtherFace.Edges));
            }
            edgesOfFace2Length.Sort();
            //if (edgesOfFace1.Count < 3 || edgesOfFace2.Count < 3) return double.PositiveInfinity;
            //edgesOfFace1 = edgesOfFace1.OrderBy(a => a.Length).ToList();
            //edgesOfFace2 = edgesOfFace2.OrderBy(a => a.Length).ToList();

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

            var r11 = edgesOfFace1Length[0]/edgesOfFace1Length[1];
            var r12 = edgesOfFace1Length[0]/edgesOfFace1Length[2];
            var r13 = edgesOfFace1Length[1]/edgesOfFace1Length[2];
            var r21 = edgesOfFace2Length[0]/edgesOfFace2Length[1];
            var r22 = edgesOfFace2Length[0]/edgesOfFace2Length[2];
            var r23 = edgesOfFace2Length[1]/edgesOfFace2Length[2];

            var similarity = Math.Abs(r11 - r21) + Math.Abs(r12 - r22) + Math.Abs(r13 - r23); // cannot exceed 3
            var areaSimilarity = 3*Math.Abs(1 - smallArea/largeArea);

            var SM = similarity + areaSimilarity;
            return SM;
        }

        /// <summary>
        /// Adds the length of the missing edge.
        /// </summary>
        /// <param name="edges">The edges.</param>
        /// <returns>System.Double.</returns>
        private static double AddMissingEdgeLength(List<Edge> edges)
        {
            Vertex ver1;
            if ((edges[0].To != edges[1].From) &&
                (edges[0].To != edges[1].To))
                ver1 = edges[0].To;
            else
                ver1 = edges[0].From;
            Vertex ver2;
            if ((edges[1].To != edges[0].From) &&
                (edges[1].To != edges[0].To))
                ver2 = edges[1].To;
            else
                ver2 = edges[1].From;
            return
                Math.Sqrt(Math.Pow(ver1.Position[0] - ver2.Position[0], 2) +
                          Math.Pow(ver1.Position[1] - ver2.Position[1], 2) +
                          Math.Pow(ver1.Position[2] - ver2.Position[2], 2));
        }


        /// <summary>
        /// Edges the classifier2.
        /// </summary>
        /// <param name="ABNProbs">The abn probs.</param>
        /// <param name="MCMProbs">The MCM probs.</param>
        /// <param name="SMProbs">The sm probs.</param>
        /// <param name="rulesArray">The rules array.</param>
        /// <param name="prob">The prob.</param>
        /// <returns>System.Int32.</returns>
        private static int EdgeClassifier2(double[] ABNProbs, double[] MCMProbs, double[] SMProbs,
            int[,] rulesArray, out double prob)
        {
            // go to the rules and and return an int corresponding to each region.
            // This function must be rewrited. It's crazy!!!!!!!!!
            prob = 0;
            var ABN = Convert.ToInt32(ABNProbs[0]);
            var MCM = Convert.ToInt32(MCMProbs[0]);
            var SM = Convert.ToInt32(SMProbs[0]);
            var t = 0;
            bool probabilityNotFound;
            do
            {
                probabilityNotFound = false;
                if (rulesArray[0, t] == ABN && rulesArray[1, t] == 10 && rulesArray[2, t] == 10)
                    prob = ABNProbs[1];
                else if (rulesArray[1, t] == MCM && rulesArray[0, t] == 10 && rulesArray[2, t] == 10)
                    prob = MCMProbs[1];
                else if (rulesArray[2, t] == SM && rulesArray[0, t] == 10 && rulesArray[1, t] == 10)
                    prob = SMProbs[1];
                else if (rulesArray[0, t] == ABN && rulesArray[1, t] == MCM && rulesArray[2, t] == 10)
                    prob = Math.Min(ABNProbs[1], MCMProbs[1]);
                else if (rulesArray[0, t] == ABN && rulesArray[1, t] == 10 && rulesArray[2, t] == SM)
                    prob = Math.Min(ABNProbs[1], SMProbs[1]);
                else if (rulesArray[0, t] == 10 && rulesArray[1, t] == MCM && rulesArray[2, t] == SM)
                    prob = Math.Min(MCMProbs[1], SMProbs[1]);
                else if (rulesArray[0, t] == ABN && rulesArray[1, t] == MCM && rulesArray[2, t] == SM)
                {
                    var m = Math.Min(ABNProbs[1], MCMProbs[1]);
                    prob = Math.Min(m, SMProbs[1]);
                }
                else probabilityNotFound = true;
            } while (probabilityNotFound && ++t < rulesArray.GetLength(1));
            if (probabilityNotFound) return 0; // t would exceed the limit, so we return 0
            return rulesArray[3, t];
        }

        /// <summary>
        /// Cats the and prob finder.
        /// </summary>
        /// <param name="metric">The metric.</param>
        /// <param name="listOfLimits">The list of limits.</param>
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        private static List<double[]> CatAndProbFinder(double metric, double[] listOfLimits)
        {
            var CatAndProb = new List<double[]>();
            //Case 1
            if (metric <= listOfLimits[0])
            {
                CatAndProb.Add(new double[] {0, 1});
                return CatAndProb;
            }
            //Case 3
            if (metric >= listOfLimits[3] && metric <= listOfLimits[4])
            {
                CatAndProb.Add(new double[] {1, 1});
                return CatAndProb;
            }
            //Case 5
            if (metric >= listOfLimits[7])
            {
                CatAndProb.Add(new double[] {2, 1});
                return CatAndProb;
            }
            //Case 2
            if (metric > listOfLimits[0] && metric < listOfLimits[3])
            {
                CatAndProb = CatAndProbForCases2and4(listOfLimits[0], listOfLimits[1], listOfLimits[2], listOfLimits[3],
                    metric, 2);
                return CatAndProb;
            }
            //Case 4
            if (metric > listOfLimits[4] && metric < listOfLimits[7])
            {
                CatAndProb = CatAndProbForCases2and4(listOfLimits[4], listOfLimits[5], listOfLimits[6], listOfLimits[7],
                    metric, 4);
                return CatAndProb;
            }


            return CatAndProb;
        }

        /// <summary>
        /// Cats the and prob for cases2and4.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="p3">The p3.</param>
        /// <param name="p4">The p4.</param>
        /// <param name="metric">The metric.</param>
        /// <param name="Case">The case.</param>
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        private static List<double[]> CatAndProbForCases2and4(double p1, double p2, double p3, double p4, double metric,
            double Case)
        {
            var catAndProb = new List<double[]>();
            var prob1 = (0 - 1)/(p2 - p1)*(metric - p1) + 1;
            var prob2 = (1 - 0)/(p4 - p3)*(metric - p3) + 1;
            if (Case == 2)
            {
                catAndProb.Add(new[] {0, prob1});
                catAndProb.Add(new[] {1, prob2});
            }
            if (Case == 4)
            {
                catAndProb.Add(new[] {1, prob1});
                catAndProb.Add(new[] {2, prob2});
            }
            return catAndProb;
        }

        #endregion

        #region Face Classification

        /// <summary>
        /// Faces the fuzzy classification.
        /// </summary>
        /// <param name="eachFace">The each face.</param>
        /// <param name="allEdgeWithScores">All edge with scores.</param>
        private static void FaceFuzzyClassification(FaceWithScores eachFace, List<EdgeWithScores> allEdgeWithScores)
        {
            /*var aya = allEdgeWithScores.Count(e => e.CatProb == null);
            var aya2 = new List<int>();
            foreach (var e in eachFace.Face.Edges)
            {
                var cddd = allEdgeWithScores.Where(ews =>
                    (ews.Edge.From.Position[0] == e.From.Position[0] && ews.Edge.From.Position[1] == e.From.Position[1] && ews.Edge.From.Position[2] == e.From.Position[2]
                    && ews.Edge.To.Position[0] == e.To.Position[0] && ews.Edge.To.Position[1] == e.To.Position[1] && ews.Edge.To.Position[2] == e.To.Position[2]) ||
                    (ews.Edge.From.Position[0] == e.To.Position[0] && ews.Edge.From.Position[1] == e.To.Position[1] && ews.Edge.From.Position[2] == e.To.Position[2]
                    && ews.Edge.To.Position[0] == e.From.Position[0] && ews.Edge.To.Position[1] == e.From.Position[1] && ews.Edge.To.Position[2] == e.From.Position[2])
                    ).ToList();
                aya2.Add(cddd.Count);
            }*/
            var t = eachFace.Face.Edges.Select(e => allEdgeWithScores.First(ews => ews.Edge == e).CatProb).ToList();
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
                            var combination = new[] {t[0].ToList()[i].Key, t[1].ToList()[j].Key, t[2].ToList()[k].Key};

                            var a = new double[3];
                            a[0] = t[0].ToList()[i].Value;
                            a[1] = t[1].ToList()[j].Value;
                            a[2] = t[2].ToList()[k].Value;

                            double totProb = 1;
                            var co = 0;

                            foreach (var ar in a.Where(p => p != 1))
                            {
                                totProb = totProb*(1 - ar);
                                co++;
                            }
                            if (co == 0)
                                totProb = 1;
                            else
                                totProb = 1 - totProb;

                            //var totProb = 1-((1-t[0].ToList()[i].Value) * (1-t[1].ToList()[j].Value) * (1-t[2].ToList()[k].Value));
                            //var combAndEdge = new Dictionary<int[], EdgeWithScores[]>();
                            //CanCom.Add(combination, totProb);

                            var edges = new[] {eachFace.Face.Edges[0], eachFace.Face.Edges[1], eachFace.Face.Edges[2]};
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

        /// <summary>
        /// Faces the classifier.
        /// </summary>
        /// <param name="bestCombination">The best combination.</param>
        /// <param name="faceRules">The face rules.</param>
        /// <returns>PrimitiveSurfaceType.</returns>
        private static PrimitiveSurfaceType FaceClassifier(int[] bestCombination, int[,] faceRules)
        {
            var intToString = new Dictionary<int, PrimitiveSurfaceType>();
            intToString.Add(200, PrimitiveSurfaceType.Flat);
            intToString.Add(201, PrimitiveSurfaceType.Cylinder);
            intToString.Add(202, PrimitiveSurfaceType.Sphere);
            intToString.Add(203, PrimitiveSurfaceType.Flat_to_Curve);
            intToString.Add(204, PrimitiveSurfaceType.Dense);
            intToString.Add(205, PrimitiveSurfaceType.Unknown);
            var sortedCom = bestCombination.OrderBy(n => n).ToArray();
            for (var i = 0; i < faceRules.GetLength(1); i++)
            {
                var arrayOfRule = new[] {faceRules[0, i], faceRules[1, i], faceRules[2, i]};
                var sortedAofR = arrayOfRule.OrderBy(n => n).ToArray();
                if (sortedCom[0] == sortedAofR[0] && sortedCom[1] == sortedAofR[1] && sortedCom[2] == sortedAofR[2])
                    return intToString[faceRules[3, i]];
            }
            return PrimitiveSurfaceType.Unknown;
        }

        /// <summary>
        /// Sortings the COM to edge dic.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>Dictionary&lt;System.Int32[], Edge[]&gt;.</returns>
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

        #region Group Faces Into Primitives

        /// <summary>
        /// Groups the faces into planning surfaces.
        /// </summary>
        /// <param name="seedFace">The seed face.</param>
        /// <param name="allFacesWithScores">All faces with scores.</param>
        /// <returns>List&lt;PlanningSurface&gt;.</returns>
        private static List<PlanningSurface> groupFacesIntoPlanningSurfaces(FaceWithScores seedFace,
            List<FaceWithScores> allFacesWithScores)
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
            if (seedFace.Face.Area < ClassificationConstants.Classifier_MinAreaForStartFace ||
                seedFace.CatToCom.Count == 0)
                return new List<PlanningSurface>();
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
                        var child = eachEdge.OwnedFace == openBranchFace.Face
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
            Message.output(
                "new patches: " + candidatePatches.Count + " -- comprised of #faces: " +
                candidatePatches.SelectMany(cp => cp.Faces).Distinct().Count(), 5);
            return candidatePatches;
        }

        /// <summary>
        /// Alreadies the searched primitive.
        /// </summary>
        /// <param name="newSeed">The new seed.</param>
        /// <param name="candidatePatches">The candidate patches.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool AlreadySearchedPrimitive(PlanningSurface newSeed, List<PlanningSurface> candidatePatches)
        {
            return candidatePatches.Any(p => p.SurfaceType == newSeed.SurfaceType
                                             && p.Faces.Contains(newSeed.Faces[0]));
        }

        #endregion

        #region Decide In Overlapping Patches

        /// <summary>
        /// Decides the on overlapping patches.
        /// </summary>
        /// <param name="surfaces">The surfaces.</param>
        /// <param name="unassignedFaces">The unassigned faces.</param>
        /// <returns>IEnumerable&lt;PlanningSurface&gt;.</returns>
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
                    for (var j = orderedSurfaces.Count - 1; j >= 0; j--)
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

        /// <summary>
        /// Res the insert.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="orderedPrimitives">The ordered primitives.</param>
        private static void ReInsert(PlanningSurface surface, List<PlanningSurface> orderedPrimitives)
        {
            var endIndex = orderedPrimitives.Count;
            if (endIndex == 0)
            {
                orderedPrimitives.Add(surface);
                return;
            }
            var startIndex = 0;
            var midIndex = endIndex/2;
            do
            {
                if (surface.Metric > orderedPrimitives[midIndex].Metric)
                    endIndex = midIndex;
                else startIndex = midIndex;
                midIndex = startIndex + (endIndex - startIndex)/2;
            } while (midIndex != endIndex && midIndex != startIndex);
            orderedPrimitives.Insert(midIndex, surface);
        }

        #endregion

        #region Make Primitives

        /// <summary>
        /// Makes the surfaces.
        /// </summary>
        /// <param name="plannedSurfaces">The planned surfaces.</param>
        /// <param name="maxFaceArea">The maximum face area.</param>
        /// <returns>List&lt;PrimitiveSurface&gt;.</returns>
        private static List<PrimitiveSurface> MakeSurfaces(List<PlanningSurface> plannedSurfaces, double maxFaceArea)
        {
            var completeSurfaces = new List<PrimitiveSurface>();

            while (plannedSurfaces.Any())
            {
                Message.output("# primitives to make: " + plannedSurfaces.Count, 5);
                var topPlannedSurface = plannedSurfaces[0];
                plannedSurfaces.RemoveAt(0);
                if (topPlannedSurface.Faces.Count < 1)
                    continue;
                if (topPlannedSurface.Faces.Count == 1)
                {
                    var face = topPlannedSurface.Faces[0].Face;
                    if (face.Area < maxFaceArea/9)
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

        /// <summary>
        /// Creates the primitive surface.
        /// </summary>
        /// <param name="topPlannedSurface">The top planned surface.</param>
        /// <returns>PrimitiveSurface.</returns>
        /// <exception cref="Exception">Cannot build Create Primitive Surface of type:  + surfaceType</exception>
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
                default:
                    throw new Exception("Cannot build Create Primitive Surface of type: " + surfaceType);
            }
        }

        /// <summary>
        /// Determines whether [is really a flat] [the specified faces].
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns><c>true</c> if [is really a flat] [the specified faces]; otherwise, <c>false</c>.</returns>
        private static bool IsReallyAFlat(IEnumerable<PolygonalFace> faces)
        {
            return ListFunctions.FacesWithDistinctNormals(faces.ToList()).Count == 1;
        }

        /// <summary>
        /// Determines whether [is really a cone] [the specified faces all].
        /// </summary>
        /// <param name="facesAll">The faces all.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="coneAngle">The cone angle.</param>
        /// <returns><c>true</c> if [is really a cone] [the specified faces all]; otherwise, <c>false</c>.</returns>
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
            for (var i = 1; i < n; i++)
                inPlaneVectors[i] = faces[i].Normal.subtract(faces[i - 1].Normal);

            var normalsOfGaussPlane = new List<double[]>();
            var tempCross = inPlaneVectors[0].crossProduct(inPlaneVectors[n - 1]).normalize();
            if (!tempCross.Any(double.IsNaN))
                normalsOfGaussPlane.Add(tempCross);
            for (var i = 1; i < n; i++)
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
                distance = -distance/n;
            }
            else
            {
                distance /= n;
                axis = normalOfGaussPlane;
            }
            coneAngle = Math.Asin(distance);
            return Math.Abs(distance) >= ClassificationConstants.MinConeGaussPlaneOffset;
        }

        /// <summary>
        /// Determines whether [is really a torus] [the specified faces].
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns><c>true</c> if [is really a torus] [the specified faces]; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool IsReallyATorus(IEnumerable<PolygonalFace> faces)
        {
            return false;
            throw new NotImplementedException();
        }

        #endregion

        #region Show Results

        /// <summary>
        /// Paints the surfaces.
        /// </summary>
        /// <param name="primitives">The primitives.</param>
        /// <param name="ts">The ts.</param>
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

        /// <summary>
        /// Reports the stats.
        /// </summary>
        /// <param name="primitives">The primitives.</param>
        /// <param name="primitivesBeforeFiltering">The primitives before filtering.</param>
        private static void ReportStats(List<PrimitiveSurface> primitives, double primitivesBeforeFiltering)
        {
            Message.output("**************** RESULTS *******************", 4);
            Message.output("Number of Primitives = " + primitives.Count, 4);
            Message.output("Number of Primitives Before Filtering= " + primitivesBeforeFiltering, 4);
            Message.output("Number of Flats = " + primitives.Count(p => p is Flat), 4);
            Message.output("Number of Cones = " + primitives.Count(p => p is Cone), 4);
            Message.output("Number of Cylinders = " + primitives.Count(p => p is Cylinder), 4);
            Message.output("Number of Spheres = " + primitives.Count(p => p is Sphere), 4);
            Message.output("Number of Torii = " + primitives.Count(p => p is Torus), 4);
            Message.output("Primitive Max Faces = " + primitives.Max(p => p.Faces.Count), 4);
            var minFaces = primitives.Min(p => p.Faces.Count);
            Message.output("Primitive Min Faces = " + minFaces, 4);
            Message.output("Number of with min faces = " + primitives.Count(p => p.Faces.Count == minFaces), 4);
            Message.output("Primitive Avg. Faces = " + primitives.Average(p => p.Faces.Count), 4);
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