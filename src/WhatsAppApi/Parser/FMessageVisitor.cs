namespace WhatsAppApi.Parser
{
    public interface FMessageVisitor
    {
        void Audio(FMessage fMessage);

        void Contact(FMessage fMessage);

        void Image(FMessage fMessage);

        void Location(FMessage fMessage);

        void System(FMessage fMessage);

        void Undefined(FMessage fMessage);

        void Video(FMessage fMessage);
    }
}