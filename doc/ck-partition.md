# Checking Partitioning

Partitioning has a log, but for the purpose of this check it can be ignored.

Checking partitioned files is as easy as diffing the original vs. the partitioned ones. The only relevant difference should be the addition of a `pb` element in the output, wherever it required partitioning; otherwise, it is copied unchanged.
