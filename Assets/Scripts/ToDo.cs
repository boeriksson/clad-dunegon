/*
    Adding a chance for shutes between rooms that overlap the same (x, z)
    Make room span several levels - adding stairs in the rooms where needed to supply this
    Add spiral stair segments that could span several levels
    Make the availiable space more dynamic - perhaps starting with a smaller area, but then widening it as needs arise!?

    Buggar:

    Done: 
    -- Adding stairs between the levels
    -- Adding diffrent levels to the dunegon
    -- Rollbacks should replace original fork with exit -1, not end with deadends!
    -- Start must be "plugged"
    -- Remove the idea of JoinSegment! Replace with join map in Join and rework to allow for room-room openings
    -- Check findpath for krocks..
    -- If dunegon ends when it's too small, new entrys should be added to working set! (if fork < 3 add 1 att appropriate ställe)
    -- Set stuff as classProps in join
    -- Straight corridors in a row should lower chance of another straight..
    -- Corridor endings should be plugged with a deadend!
    -- Corridor merge should not overlay existing space with a straight - must replace adjoining section with a section exit +1 
    -- New JoinSegment - should add nessecary segments starting with a straight, to end at a predefined "join" coordinate. Coord before "join" is the new exit
    -- Rollbacked corridors should end with a deadend
    -- After hitting the indexOutOfBounds of the levelMap, corridors are backedout and exits are NOT plugged..
    -- Buggar: 
        - Must clear segments/exits from workingSet on backout (remake as dict?) 
        - Need to ensure that "joiningSide" always is correct - cannot make dec based on joinSegment.GlobalDirection!
        - Room that is joiningSegment (green) lack one exit? 
        - Ibland skrivs korridorer över? (bara backedOut and replaced??)
        - Efter backout and removewithonelessexit, the old "tiles" are still there - 2 sets of tiles (room only?) - Happends when there are 1 or more backouts... 
        - Krock med annan scan backar ut ena parten, men den andra stannar halvfärdig
        - Backing out to a segment with 2 exits - need to check that there segments at the other exit, else continue backing out.. 
        - RedoSegmentWithOneLessExit segment not found... backing out of "joinsegment" 
        - Joinsegment för nära join så att addSegment börjar på sidan av join
        - Join till hörn av rum direction -x, så läggs den nya exiten till, men det gamla hörnet finns kvar och stänger exiten?
        - Fail to create exit in large room.. (-z)
        - After backout and remaderoomwith1lessexit - check that all exits are populated, else redo again! 
        - Vid en join med replaceSegmentWithNewExit, där det gamla segmentet fortfarande ligger i workingSet så sker en utbackning på den tråden, som lämnar en tarm med 2 öppningar!
*/
