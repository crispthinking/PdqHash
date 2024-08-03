using PdqHash.Hashing.Extensions;

namespace PdqHash.Hashing.Video;

public class VPdqComparer
{
    /// <summary>
    /// Calculates whether to sets of video PDQ files should be considered a match.
    /// </summary>
    /// <remarks>
    /// </remarks> 
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="distance">The PDQ match distance for each frame</param>
    /// <param name="matchThreshold">The query match percentage threshold Pq.
    /// Using a higher threshold will exclude videos with "extra" frames or content. 
    /// 0% means don't exclude matches based on padding in the uploaded video.
    /// Using Pc = 100% and Pq = 100% will attempt to find only videos with the exact same frame content.
    /// </param>
    /// <returns></returns>
    public static bool CalculateMatch(IReadOnlySet<VPdqHash> left, IReadOnlySet<VPdqHash> right, int distance, double matchThreshold)
    {
        var leftFramesMatched = 0;
        foreach (var leftFrame in left)
        {
            foreach (var rightFrame in right)
            {
                if (leftFrame.Hash.hammingDistance(rightFrame.Hash) < distance)
                {
                    leftFramesMatched++;
                    break;
                }
            }
        }

        var rightFramesMatched = 0;
        foreach (var rightFrame in right)
        {
            foreach (var leftFrame in left)
            {
                if (rightFrame.Hash.hammingDistance(leftFrame.Hash) < distance)
                {
                    rightFramesMatched++;
                    break;
                }
            }
        }

        var leftMatchPercent = leftFramesMatched * 100 / left.Count;
        var rightMatchPercent = rightFramesMatched * 100 / right.Count;

        return leftMatchPercent > matchThreshold || rightMatchPercent > matchThreshold;
    }

    /// <summary>
    /// Calculates whether to sets of video PDQ files should be considered a match.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="distance">The PDQ match distance for each frame</param>
    /// <param name="matchThreshold">The query match percentage threshold Pq.
    /// Using a higher threshold will exclude videos with "extra" frames or content. 
    /// 0% means don't exclude matches based on padding in the uploaded video.
    /// Using Pc = 100% and Pq = 100% will attempt to find only videos with the exact same frame content.
    /// </param>
    /// <returns></returns>
    public static async Task<(bool Match, double LeftMatchPercent, double RightMatchPercent)>
        CalculateMatchAsync(IAsyncEnumerable<VPdqHash> left, IAsyncEnumerable<VPdqHash> right, int distance, double matchThreshold)
    {
        var leftFramesMatched = 0;

        var l = new CachedAsyncEnumerable<VPdqHash>(left);
        var r = new CachedAsyncEnumerable<VPdqHash>(right);

        await foreach (var leftFrame in l)
        {
            await foreach (var rightFrame in r)
            {
                if (leftFrame.Hash.hammingDistance(rightFrame.Hash) < distance)
                {
                    leftFramesMatched++;
                    break;
                }
            }
        }

        var rightFramesMatched = 0;
        await foreach (var rightFrame in r)
        {
            await foreach (var leftFrame in l)
            {
                if (rightFrame.Hash.hammingDistance(leftFrame.Hash) < distance)
                {
                    rightFramesMatched++;
                    break;
                }
            }
        }

        var leftMatchPercent = leftFramesMatched * 100 / await l.CountAsync();
        var rightMatchPercent = rightFramesMatched * 100 / await r.CountAsync();

        return (leftMatchPercent > matchThreshold || rightMatchPercent > matchThreshold, leftMatchPercent, rightMatchPercent);
    }
}