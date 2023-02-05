/*
    Straight corridors in a row should lower chance of another straight..
    Corridor endings should be plugged with a deadend!
    Start must be "plugged"
    If dunegon ends when it's too small, new entrys should be added to working set!
    Rollbacks should replace original fork with exit -1, not end with deadends!

    -- Corridor merge should not overlay existing space with a straight - must replace adjoining section with a section exit +1 
    -- New JoinSegment - should add nessecary segments starting with a straight, to end at a predefined "join" coordinate. Coord before "join" is the new exit
    -- Rollbacked corridors should end with a deadend
*/
