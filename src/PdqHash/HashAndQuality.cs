namespace PdqHash;

public record HashResult(PdqHash256 Hash, int Quality, HashingStatistics Statistics);

public record HashingStatistics(double ReadSeconds, double HashSeconds, int ImageHeightTimesWidth, string Source);

internal sealed record HashAndQuality(PdqHash256 Hash, int Quality);

public record HashesAndQuality(
    PdqHash256 Hash,
    PdqHash256 HashRotate90,
    PdqHash256 HashRotate180,
    PdqHash256 HashRotate270,
    PdqHash256 HashFlipX,
    PdqHash256 HashFlipY,
    PdqHash256 HashFlipPlus1,
    PdqHash256 HashFlipMinus1,
    int Quality
);