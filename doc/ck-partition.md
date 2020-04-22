# Checking Partitioning

Partitioning has a log, but for the purpose of this check it can be ignored.

Checking partitioned files is as easy as diffing the original vs. the partitioned ones. The only relevant difference should be the addition of a `pb` element in the output, wherever it required partitioning; otherwise, it is copied unchanged.

For instance, I'm using the Diff tool which comes with Oxygen. In this case, I recommend to load both documents, and then under the `Compare` menu select `Format and Indent Both Files` before comparing. This removes unrelevant differences connected to formatting, allowing for a much clearer comparison.
