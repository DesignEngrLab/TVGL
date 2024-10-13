namespace PointCloud;

/// <summary>
/// The MinimumEnclosure class includes static functions for defining smallest enclosures for a
/// tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
/// </summary>
public static class MinimumCircleClass
{
    /// <summary>
    /// Finds the minimum bounding circle
    /// </summary>
    /// <param name="points">The points.</param>
    /// <returns>System.Double.</returns>
    /// <exception cref="Exception">Bounding circle failed to converge</exception>
    /// <references>
    /// Based on Emo Welzl's "move-to-front heuristic" and this paper (algorithm 1).
    /// http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
    /// This algorithm runs in near linear time. Visiting most points just a few times.
    /// </references>
    public static Circle MinimumCircle(this IEnumerable<Vector2> pointsInput)
    {
        var points = pointsInput.ToArray();
        var numPoints = points.Length;
        var maxNumStalledIterations = 10; // why 10? it was (int)(1.1 * numPoints);
        // since the circle can be made up of at most 3 points, we can just check for that
        // there is an oscillation between two or more points that would all be a index-4.
        // worst case scenario there are 5 points that are all on the circle and all "appear"
        // outside of the circle when they aren't main contributors to it (in positions 0,1,or 2)
        // so cycling twice through this list or 10 times is more than sufficient
        if (numPoints == 0)
            throw new ArgumentException("No points provided.");
        else if (numPoints == 1)
            return new Circle(points[0], 0.0);
        else if (numPoints == 2)
            return Circle.CreateFrom2Points(points[0], points[1]);

        // make a circle from the first three points
        var circle = FirstCircle(points);
        var startIndex = 3;
        var maxDistSqared = circle.RadiusSquared;
        bool newPointFoundOutsideCircle;
        var stallCounter = 0;
        var indexOfMaxDist = -1;
        do
        {
            newPointFoundOutsideCircle = false;
            for (int i = startIndex; i < numPoints; i++)
            {
                var dist = (points[i] - circle.Center).LengthSquared();

                if (dist > maxDistSqared)
                {
                    maxDistSqared = dist;
                    if (indexOfMaxDist == i) stallCounter++;
                    else stallCounter = 0;
                    indexOfMaxDist = i;
                    newPointFoundOutsideCircle = true;
                }
            }
            if (newPointFoundOutsideCircle)
            {
                //Console.WriteLine(indexOfMaxDist+", "+maxDistSqared);
                var maxPoint = points[indexOfMaxDist];
                Array.Copy(points, 0, points, 1, indexOfMaxDist);
                points[0] = maxPoint;
                circle = FindCircle(points);
                maxDistSqared = circle.RadiusSquared;
                startIndex = 4;
                // should we start at 3 or 4? initially the circle was defined with the first 2 or 3 points.
                // (if it were 2 then the third point was inside the circle and was ineffecitve).
                // but these indices would be 0,1,2 - so shouldn't the next point to check be 3?!
                // no, because when the new point was moved to the front of the list, the least
                // contributor would have been at index-2, and now that's index-3 (this is done in the
                // FindCircle function), so we don't need to check it again. FindCircle, swapped points in
                // the first four positions (0,1,2,3) so that the defining circle was made by 0,1 & 2.
            }
        } while (newPointFoundOutsideCircle && stallCounter < maxNumStalledIterations);
        return circle;
    }

    private static Circle FirstCircle(Vector2[] points)
    {
        // during the main loop, the most outside point will be moved to the front
        // of the list. As can be seen in FindCircle, this greatly reduces the number
        // of circles to check. However, we do not have that luxury at first (the luxury
        // of knowing which point is part of the new circle). To prevent complicated
        // FindCircle, we can make a circle from the first 3 points - and check all four
        // permutations in this function. This will ensure that FindCircle runs faster (without
        // extra conditions and ensures that it won't miss a case
        var circle = Circle.CreateFrom2Points(points[0], points[1]);
        if ((points[2] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            return circle;
        circle = Circle.CreateFrom2Points(points[0], points[2]);
        if ((points[1] - circle.Center).LengthSquared() <= circle.RadiusSquared)
        {
            // since 0 and 2 are furthest apart, we need to swap 1 and 2
            // so that the two points in the circle are at the beinning of the list
            Constants.SwapItemsInList(1, 2, points);
            return circle;
        }
        circle = Circle.CreateFrom2Points(points[1], points[2]);
        if ((points[0] - circle.Center).LengthSquared() <= circle.RadiusSquared)
        {
            // since 1 and 2 are furthest apart, we need to swap 0 and 2
            // so that the two points in the circle are at the beinning of the list
            Constants.SwapItemsInList(0, 2, points);
            return circle;
        }
        // otherwise, it's the 3-point circle
        Circle.CreateFrom3Points(points[0], points[1], points[2], out circle);
        return circle;
    }
    private static Circle FindCircle(Vector2[] points)
    {
        // we know that 1,2,3 defined (were encompassed by) the last circle
        // the new 0 is outside of the 1-2-3 circle
        // so we need to
        // 1. make the 0-1 circle and check with 2 & 3
        // 2. make the 0-2 circle and check with 1 & 3
        // 3. make the 0-3 circle and check with 1 & 2
        // 4. make the 0-1-2 circle and check with 3 
        // 5. make the 0-1-3 circle and check with 2
        // 6. make the 0-2-3 circle and check with 1
        // for the latter 3 we want to return the smallest that includes the 4th point

        // 1. make the 0-1 circle and check with 2 & 3
        var circle = Circle.CreateFrom2Points(points[0], points[1]);
        if ((points[2] - circle.Center).LengthSquared() <= circle.RadiusSquared
            && (points[3] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            return circle;

        // 2. make the 0-2 circle and check with 1 & 3
        circle = Circle.CreateFrom2Points(points[0], points[2]);
        if ((points[1] - circle.Center).LengthSquared() <= circle.RadiusSquared
            && (points[3] - circle.Center).LengthSquared() <= circle.RadiusSquared)
        {
            Constants.SwapItemsInList(1, 2, points);
            return circle;
        }
        // 3. make the 0-3 circle and check with 1 & 2
        circle = Circle.CreateFrom2Points(points[0], points[3]);
        if ((points[1] - circle.Center).LengthSquared() <= circle.RadiusSquared
            && (points[2] - circle.Center).LengthSquared() <= circle.RadiusSquared)
        {
            Constants.SwapItemsInList(1, 3, points);
            return circle;
        }

        Circle tempCircle;
        // circle 0-1-2
        var minRadiusSqd = double.PositiveInfinity;
        if (Circle.CreateFrom3Points(points[0], points[1], points[2], out tempCircle)
            && !(points[3] - circle.Center).LengthSquared().IsGreaterThanNonNegligible(circle.RadiusSquared))
        { // this one uses IsGreaterThanNonNegligible to prevent infinite cycling when more points are on the circle
            circle = tempCircle;
            minRadiusSqd = circle.RadiusSquared;
        }
        // circle 0-1-3
        var swap3And2 = false;
        if (Circle.CreateFrom3Points(points[0], points[1], points[3], out tempCircle)
            && (points[2] - tempCircle.Center).LengthSquared() <= tempCircle.RadiusSquared
            && tempCircle.RadiusSquared < minRadiusSqd)
        {
            swap3And2 = true;
            circle = tempCircle;
            minRadiusSqd = circle.RadiusSquared;
        }
        // circle 0-2-3
        var swap3And1 = false;
        if (Circle.CreateFrom3Points(points[0], points[2], points[3], out tempCircle)
            && (points[1] - tempCircle.Center).LengthSquared() <= tempCircle.RadiusSquared
            && tempCircle.RadiusSquared < minRadiusSqd)
        {
            swap3And1 = true;
            circle = tempCircle;
        }
        if (swap3And1) Constants.SwapItemsInList(3, 1, points);
        else if (swap3And2) Constants.SwapItemsInList(3, 2, points);
        return circle;
    }


}