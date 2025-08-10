# Social Animal - Bootstrap Domain Model & Admin Panel

Social animal exists to automate the workflow of hosting an event.
You decide who is invited, what the event is, where it is etc; but then you need to issue invitations.
I've handled invitations manually, but it's a lot of chasing people around & difficult to remember.

So with social animal instead I'll create an Event, which will have an invite link I can share.
Guests will click on a link and RSVP that they're coming (or not).
And we can all view an Event specific page detailing who is coming.

This particular project is setup the initial database models and an admin panel to view & edit these entities.

Phase 1 -- initial schema

Any row named Slug must have a global unique index for the table.  Slugs are opaque, public identifiers.

Event (
    Id: primary key
    Slug: string
    Title: string
    AddressLine1: string
    AddressLine2: string, nullable
    City: string
    State: two digit state code
    Postal: string

    CreatedAt, UpdatedAt, DeletedAt: nullable
)

Invitation (
    Id: primary key
    Slug: string -- public facing opaque identifier
    EventId: fk to Event

    CreatedAt, UpdatedAt, DeletedAt: nullable
)

Reservation (
    Id: primary key
    InvitationId: fk on Invitation
    UserId: fk on User
    PartySize: u32, 0 = sends regrets
)

User (
    Id: primary key
    Slug: string
    FirstName: string
    LastName: string
    phone: string, but expected to be in E164 format e.g. "+14256987637"

    CreatedAt, UpdatedAt, DeletedAt: nullable
)

Phase 2 -- Admin portal

Standup an ASP.net project which will be the internal facing admin portal.
This should be a multi-page, server side rendered application.  
Use HTMX to achieve a level of interactiveness without constant page refreshes.

Do not worry about authentication; this will be protected initially via network segmentation.

The portal must have the following structure:

/index.html
    Links to summary pages for each entity type in the database (Event, User, Invitation, Reservation)
    
/entities/{entity_name}/index.html, e.g. /entities/invitations/index.html
    Paginated list views, with filters allowing me to sift through entities.

/entities/{entity_name}/{slug}.html, e.g. /entities/events/abcdef.html
    Detailed view for a given entity type, lists _all_ information of the entity.
    Includes links to any other entities that have a FK relationship
